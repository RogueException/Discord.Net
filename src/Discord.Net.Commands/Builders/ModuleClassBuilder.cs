using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Discord.Commands.Builders;

namespace Discord.Commands
{
    internal static class ModuleClassBuilder
    {
        private static readonly TypeInfo ModuleTypeInfo = typeof(IModuleBase).GetTypeInfo();

        public static async Task<IReadOnlyList<TypeInfo>> SearchAsync(Assembly assembly, CommandService service)
        {
            bool IsLoadableModule(TypeInfo info)
            {
                return info.DeclaredMethods.Any(x => x.GetCustomAttribute<CommandAttribute>() != null) &&
                    info.GetCustomAttribute<DontAutoLoadAttribute>() == null;
            }

            var result = new List<TypeInfo>();

            foreach (var typeInfo in assembly.DefinedTypes)
            {
                if (typeInfo.IsPublic || typeInfo.IsNestedPublic)
                {
                    if (IsValidModuleDefinition(typeInfo) &&
                        !typeInfo.IsDefined(typeof(DontAutoLoadAttribute)))
                    {
                        result.Add(typeInfo);
                    }
                }
                else if (IsLoadableModule(typeInfo))
                {
                    await service._cmdLogger.WarningAsync($"Class {typeInfo.FullName} is not public and cannot be loaded. To suppress this message, mark the class with {nameof(DontAutoLoadAttribute)}.").ConfigureAwait(false);
                }
            }

            return result;
        }


        public static Task<Dictionary<Type, ModuleInfo>> BuildAsync(CommandService service, IServiceProvider services, params TypeInfo[] validTypes) => BuildAsync(validTypes, service, services);
        public static async Task<Dictionary<Type, ModuleInfo>> BuildAsync(IEnumerable<TypeInfo> validTypes, CommandService service, IServiceProvider services)
        {
            /*if (!validTypes.Any())
                throw new InvalidOperationException("Could not find any valid modules from the given selection");*/

            var topLevelGroups = validTypes.Where(x => x.DeclaringType == null || !IsValidModuleDefinition(x.DeclaringType.GetTypeInfo()));

            var builtTypes = new List<TypeInfo>();

            var result = new Dictionary<Type, ModuleInfo>();

            foreach (var typeInfo in topLevelGroups)
            {
                // TODO: This shouldn't be the case; may be safe to remove?
                if (result.ContainsKey(typeInfo.AsType()))
                    continue;

                var module = new ModuleBuilder(service, null);

                BuildModule(module, typeInfo, service, services);
                BuildSubTypes(module, typeInfo.DeclaredNestedTypes, builtTypes, service, services);
                builtTypes.Add(typeInfo);

                result[typeInfo.AsType()] = module.Build(service, services);
            }

            await service._cmdLogger.DebugAsync($"Successfully built {builtTypes.Count} modules.").ConfigureAwait(false);

            return result;
        }

        private static void BuildSubTypes(ModuleBuilder builder, IEnumerable<TypeInfo> subTypes, List<TypeInfo> builtTypes, CommandService service, IServiceProvider services)
        {
            foreach (var typeInfo in subTypes)
            {
                if (!IsValidModuleDefinition(typeInfo))
                    continue;
                
                if (builtTypes.Contains(typeInfo))
                    continue;
                
                builder.AddModule((module) => 
                {
                    BuildModule(module, typeInfo, service, services);
                    BuildSubTypes(module, typeInfo.DeclaredNestedTypes, builtTypes, service, services);
                });

                builtTypes.Add(typeInfo);
            }
        }

        private static void BuildModule(ModuleBuilder builder, TypeInfo typeInfo, CommandService service, IServiceProvider services)
        {
            var attributes = typeInfo.GetCustomAttributes();
            builder.TypeInfo = typeInfo;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case NameAttribute name:
                        builder.Name = name.Text;
                        break;
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case RemarksAttribute remarks:
                        builder.Remarks = remarks.Text;
                        break;
                    case AliasAttribute alias:
                        builder.AddAliases(alias.Aliases);
                        break;
                    case GroupAttribute group:
                        builder.Name = builder.Name ?? group.Prefix;
                        builder.Group = group.Prefix;
                        builder.AddAliases(group.Prefix);
                        break;
                    case PreconditionAttribute precondition:
                        builder.AddPrecondition(precondition);
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }

            //Check for unspecified info
            if (builder.Aliases.Count == 0)
                builder.AddAliases("");
            if (builder.Name == null)
                builder.Name = typeInfo.Name;

            // Get all methods (including from inherited members), that are valid commands
            var validCommands = typeInfo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(IsValidCommandDefinition);

            foreach (var method in validCommands)
            {
                builder.AddCommand((command) => 
                {
                    BuildCommand(command, typeInfo, method, service, services);
                });
            }
        }

        private static void BuildCommand(CommandBuilder builder, TypeInfo typeInfo, MethodInfo method, CommandService service, IServiceProvider serviceprovider)
        {
            var attributes = method.GetCustomAttributes();
            
            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case CommandAttribute command:
                        builder.AddAliases(command.Text);
                        builder.RunMode = command.RunMode;
                        builder.Name = builder.Name ?? command.Text;
                        builder.IgnoreExtraArgs = command.IgnoreExtraArgs ?? service._ignoreExtraArgs;
                        break;
                    case NameAttribute name:
                        builder.Name = name.Text;
                        break;
                    case PriorityAttribute priority:
                        builder.Priority = priority.Priority;
                        break;
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case RemarksAttribute remarks:
                        builder.Remarks = remarks.Text;
                        break;
                    case AliasAttribute alias:
                        builder.AddAliases(alias.Aliases);
                        break;
                    case PreconditionAttribute precondition:
                        builder.AddPrecondition(precondition);
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }

            if (builder.Name == null)
                builder.Name = method.Name;

            var parameters = method.GetParameters();
            int pos = 0, count = parameters.Length;
            foreach (var paramInfo in parameters)
            {
                builder.AddParameter((parameter) => 
                {
                    BuildParameter(parameter, paramInfo, pos++, count, service, serviceprovider);
                });
            }

            var createInstance = ReflectionUtils.CreateBuilder<IModuleBase>(typeInfo, service);

            async Task<IResult> ExecuteCallback(ICommandContext context, object[] args, IServiceProvider services, CommandInfo cmd)
            {
                var instance = createInstance(services);
                instance.SetContext(context);

                try
                {
                    instance.BeforeExecute(cmd);

                    var task = method.Invoke(instance, args) as Task ?? Task.Delay(0);
                    if (task is Task<RuntimeResult> resultTask)
                    {
                        return await resultTask.ConfigureAwait(false);
                    }
                    else
                    {
                        await task.ConfigureAwait(false);
                        return ExecuteResult.FromSuccess();
                    }
                }
                finally
                {
                    instance.AfterExecute(cmd);
                    (instance as IDisposable)?.Dispose();
                }
            }

            builder.Callback = ExecuteCallback;
        }

        private static void BuildParameter(ParameterBuilder builder, System.Reflection.ParameterInfo paramInfo, int position, int count, CommandService service, IServiceProvider services)
        {
            var attributes = paramInfo.GetCustomAttributes();
            var paramType = paramInfo.ParameterType;

            builder.Name = paramInfo.Name;

            builder.IsOptional = paramInfo.IsOptional;
            builder.DefaultValue = paramInfo.HasDefaultValue ? paramInfo.DefaultValue : null;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case OverrideTypeReaderAttribute typeReader:
                        builder.TypeReader = GetTypeReader(service, paramType, typeReader.TypeReader, services);
                        break;
                    case ParamArrayAttribute _:
                        builder.IsMultiple = true;
                        paramType = paramType.GetElementType();
                        break;
                    case ParameterPreconditionAttribute precon:
                        builder.AddPrecondition(precon);
                        break;
                    case NameAttribute name:
                        builder.Name = name.Text;
                        break;
                    case RemainderAttribute _:
                        if (position != count - 1)
                            throw new InvalidOperationException($"Remainder parameters must be the last parameter in a command. Parameter: {paramInfo.Name} in {paramInfo.Member.DeclaringType.Name}.{paramInfo.Member.Name}");

                        builder.IsRemainder = true;
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }

            builder.ParameterType = paramType;

            if (builder.TypeReader == null)
            {
                builder.TypeReader = service.GetTypeReaders(paramType, false)?.FirstOrDefault().Value
                    ?? service.GetDefaultTypeReader(paramType);
            }
        }

        internal static TypeReader GetTypeReader(CommandService service, Type paramType, Type typeReaderType, IServiceProvider services)
        {
            var readers = service.GetTypeReaders(paramType, true);
            if (readers != null)
                foreach (var kvp in readers)
                    if (kvp.Key == typeReaderType)
                        return kvp.Value;

            //We dont have a cached type reader, create one
            TypeReader reader = ReflectionUtils.CreateObject<TypeReader>(typeReaderType.GetTypeInfo(), service, services);
            reader.IsOverride = true;
            service.AddTypeReader(paramType, reader);

            return reader;
        }

        private static bool IsValidModuleDefinition(TypeInfo typeInfo)
        {
            return ModuleTypeInfo.IsAssignableFrom(typeInfo) &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.ContainsGenericParameters;
        }

        private static bool IsValidCommandDefinition(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(CommandAttribute)) &&
                   (methodInfo.ReturnType == typeof(Task) || methodInfo.ReturnType == typeof(Task<RuntimeResult>)) &&
                   !methodInfo.IsStatic &&
                   !methodInfo.IsGenericMethod;
        }
    }
}
