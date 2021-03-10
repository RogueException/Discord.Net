using Discord.Commands.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    internal static class SlashCommandServiceHelper
    {
        /// <summary>
        /// Get all of the valid user-defined slash command modules 
        /// </summary>
        public static async Task<IReadOnlyList<TypeInfo>> GetValidModuleClasses(Assembly assembly, SlashCommandService service)
        {
            var result = new List<TypeInfo>();

            foreach (TypeInfo typeInfo in assembly.DefinedTypes)
            {
                if (IsValidModuleDefinition(typeInfo))
                {
                    // To simplify our lives, we need the modules to be public.
                    if (typeInfo.IsPublic || typeInfo.IsNestedPublic)
                    {
                        result.Add(typeInfo);
                    }
                    else
                    {
                        await service._logger.WarningAsync($"Found class {typeInfo.FullName} as a valid SlashCommand Module, but it's not public!");
                    }
                }
            }

            return result;
        }
        private static bool IsValidModuleDefinition(TypeInfo typeInfo)
        {
            // See if the base type (SlashCommandInfo<T>) implements interface ISlashCommandModule
            return typeInfo.BaseType.GetInterfaces()
                .Any(n => n == typeof(ISlashCommandModule)) &&
                typeInfo.GetCustomAttributes(typeof(CommandGroup)).Count() == 0;
        }

        /// <summary>
        /// Create an instance of each user-defined module
        /// </summary>
        public static async Task<Dictionary<Type, SlashModuleInfo>> InstantiateModules(IReadOnlyList<TypeInfo> types, SlashCommandService slashCommandService)
        {
            var result = new Dictionary<Type, SlashModuleInfo>();
            // Here we get all modules thate are NOT sub command groups and instantiate them.
            foreach (Type userModuleType in types)
            {
                SlashModuleInfo moduleInfo = new SlashModuleInfo(slashCommandService);
                moduleInfo.SetType(userModuleType);

                // If they want a constructor with different parameters, this is the place to add them.
                object instance = userModuleType.GetConstructor(Type.EmptyTypes).Invoke(null);
                moduleInfo.SetCommandModule(instance);
                moduleInfo.isGlobal = IsCommandModuleGlobal(userModuleType);

                moduleInfo.SetSubCommandGroups(InstantiateSubModules(userModuleType, moduleInfo, slashCommandService));
                result.Add(userModuleType, moduleInfo);
            }
            return result;
        }
        public static List<SlashModuleInfo> InstantiateSubModules(Type rootModule,SlashModuleInfo rootModuleInfo, SlashCommandService slashCommandService)
        {
            // Instantiate all of the nested modules.
            List<SlashModuleInfo> commandGroups = new List<SlashModuleInfo>();
            foreach(Type commandGroupType in rootModule.GetNestedTypes())
            {
                if(TryGetCommandGroupAttribute(commandGroupType, out CommandGroup commandGroup))
                {
                    SlashModuleInfo groupInfo = new SlashModuleInfo(slashCommandService);
                    groupInfo.SetType(commandGroupType);

                    object instance = commandGroupType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    groupInfo.SetCommandModule(instance);

                    groupInfo.MakeCommandGroup(commandGroup,rootModuleInfo);
                    groupInfo.MakePath();
                    groupInfo.isGlobal = IsCommandModuleGlobal(commandGroupType);

                    groupInfo.SetSubCommandGroups(InstantiateSubModules(commandGroupType, groupInfo, slashCommandService));
                    commandGroups.Add(groupInfo);
                }
            }
            return commandGroups;
        }
        public static bool TryGetCommandGroupAttribute(Type module, out CommandGroup commandGroup)
        {
            if(!module.IsPublic && !module.IsNestedPublic)
            {
                commandGroup = null;
                return false;
            }

            var commandGroupAttributes = module.GetCustomAttributes(typeof(CommandGroup));
            if( commandGroupAttributes.Count() == 0)
            {
                commandGroup = null;
                return false;
            }
            else if(commandGroupAttributes.Count() > 1)
            {
                throw new Exception($"Too many CommandGroup attributes on a single class ({module.FullName}). It can only contain one!");
            }
            else
            {
                commandGroup = commandGroupAttributes.First() as CommandGroup;
                return true;
            }
        }
        public static bool IsCommandModuleGlobal(Type userModuleType)
        {
            // Verify that we only have one [Global] attribute
            IEnumerable<Attribute> slashCommandAttributes = userModuleType.GetCustomAttributes(typeof(Global));
            if (slashCommandAttributes.Count() > 1)
            {
                throw new Exception("Too many Global attributes on a single method. It can only contain one!");
            }
            // And at least one
            if (slashCommandAttributes.Count() == 0)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Prepare all of the commands and register them internally.
        /// </summary>
        public static async Task<Dictionary<string, SlashCommandInfo>> CreateCommandInfos(IReadOnlyList<TypeInfo> types, Dictionary<Type, SlashModuleInfo> moduleDefs, SlashCommandService slashCommandService)
        {
            // Create the resulting dictionary ahead of time
            var result = new Dictionary<string, SlashCommandInfo>();
            // For each user-defined module ...
            foreach (var userModule in types)
            {
                // Get its associated information. If there isn't any it means something went wrong, but it's not a critical error.
                SlashModuleInfo moduleInfo;
                if (moduleDefs.TryGetValue(userModule, out moduleInfo))
                {
                    // Create the root-level commands
                    var commandInfos = CreateSameLevelCommands(result, userModule, moduleInfo);
                    moduleInfo.SetCommands(commandInfos);
                    // Then create all of the command groups it has.
                    CreateSubCommandInfos(result, moduleInfo.commandGroups, slashCommandService);
                }
            }
            return result;
        }
        public static void CreateSubCommandInfos(Dictionary<string, SlashCommandInfo> result, List<SlashModuleInfo> subCommandGroups, SlashCommandService slashCommandService)
        {
            foreach (var subCommandGroup in subCommandGroups)
            {
                // Create the commands that is on the same hierarchical level as this ...
                var commandInfos = CreateSameLevelCommands(result, subCommandGroup.moduleType.GetTypeInfo(), subCommandGroup);
                subCommandGroup.SetCommands(commandInfos);

                // ... and continue with the lower sub command groups.
                CreateSubCommandInfos(result, subCommandGroup.commandGroups, slashCommandService);
            }
        }
        private static List<SlashCommandInfo> CreateSameLevelCommands(Dictionary<string, SlashCommandInfo> result, TypeInfo userModule, SlashModuleInfo moduleInfo)
        {
            var commandMethods = userModule.GetMethods();
            List<SlashCommandInfo> commandInfos = new List<SlashCommandInfo>();
            foreach (var commandMethod in commandMethods)
            {
                // Get the SlashCommand attribute
                SlashCommand slashCommand;
                if (IsValidSlashCommand(commandMethod, out slashCommand))
                {
                    // Create the delegate for the method we want to call once the user interacts with the bot.
                    // We use a delegate because of the unknown number and type of parameters we will have.
                    Delegate delegateMethod = CreateDelegate(commandMethod, moduleInfo.userCommandModule);

                    SlashCommandInfo commandInfo = new SlashCommandInfo(
                        module: moduleInfo,
                        name: slashCommand.commandName,
                        description: slashCommand.description,
                        // Generate the parameters. Due to it's complicated way the algorithm has been moved to its own function.
                        parameters: ConstructCommandParameters(commandMethod),
                        userMethod: delegateMethod,
                        isGlobal: IsCommandGlobal(commandMethod)
                        );

                    result.Add(commandInfo.Module.Path + SlashModuleInfo.PathSeperator + commandInfo.Name, commandInfo);
                    commandInfos.Add(commandInfo);
                }
            }

            return commandInfos;
        }

        /// <summary>
        /// Determines wheater a method can be clasified as a slash command
        /// </summary>
        private static bool IsValidSlashCommand(MethodInfo method, out SlashCommand slashCommand)
        {
            // Verify that we only have one [SlashCommand(...)] attribute
            IEnumerable<Attribute> slashCommandAttributes = method.GetCustomAttributes(typeof(SlashCommand));
            if (slashCommandAttributes.Count() > 1)
            {
                throw new Exception("Too many SlashCommand attributes on a single method. It can only contain one!");
            }
            // And at least one
            if (slashCommandAttributes.Count() == 0)
            {
                slashCommand = null;
                return false;
            }
            // And return the first (and only) attribute
            slashCommand = slashCommandAttributes.First() as SlashCommand;
            return true;
        }
        /// <summary>
        /// Determins if the method has a [Global] Attribute.
        /// </summary>
        private static bool IsCommandGlobal(MethodInfo method)
        {
            // Verify that we only have one [Global] attribute
            IEnumerable<Attribute> slashCommandAttributes = method.GetCustomAttributes(typeof(Global));
            if (slashCommandAttributes.Count() > 1)
            {
                throw new Exception("Too many Global attributes on a single method. It can only contain one!");
            }
            // And at least one
            if (slashCommandAttributes.Count() == 0)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Process the parameters of this method, including all the attributes.
        /// </summary>
        private static List<SlashParameterInfo> ConstructCommandParameters(MethodInfo method)
        {
            // Prepare the final list of parameters
            List<SlashParameterInfo> finalParameters = new List<SlashParameterInfo>();

            // For each mehod parameter ...
            // ex: ... MyCommand(string abc, int myInt)
            // `abc` and `myInt` are parameters
            foreach (var methodParameter in method.GetParameters())
            {
                SlashParameterInfo newParameter = new SlashParameterInfo();

                // Test for the [ParameterName] Attribute. If we have it, then use that as the name,
                // if not just use the parameter name as the option name.
                var customNameAttributes = methodParameter.GetCustomAttributes(typeof(ParameterName));
                if (customNameAttributes.Count() == 0)
                    newParameter.Name = methodParameter.Name;
                else if (customNameAttributes.Count() > 1)
                    throw new Exception($"Too many ParameterName attributes on a single parameter ({method.Name} -> {methodParameter.Name}). It can only contain one!");
                else
                    newParameter.Name = (customNameAttributes.First() as ParameterName).name;

                // Get to see if it has a Description Attribute.
                // If it has
                // 0 -> then use the default description
                // 1 -> Use the value from that attribute
                // 2+ -> Throw an error. This shouldn't normaly happen, but we check for sake of sanity
                var descriptions = methodParameter.GetCustomAttributes(typeof(Description));
                if (descriptions.Count() == 0)
                    newParameter.Description = Description.DefaultDescription;
                else if (descriptions.Count() > 1)
                    throw new Exception($"Too many Description attributes on a single parameter ({method.Name} -> {methodParameter.Name}). It can only contain one!");
                else
                    newParameter.Description = (descriptions.First() as Description).description;

                // Set the Type of the parameter.
                // In the case of int and int? it returns the same type - INTEGER.
                // Same with bool and bool?.
                newParameter.Type = TypeFromMethodParameter(methodParameter);

                // If we have a nullble type (int? or bool?) mark it as such.
                newParameter.Nullable = GetNullableStatus(methodParameter);

                // Test for the [Required] Attribute
                var requiredAttributes = methodParameter.GetCustomAttributes(typeof(Required));
                if (requiredAttributes.Count() == 1)
                    newParameter.Required = true;
                else if (requiredAttributes.Count() > 1)
                    throw new Exception($"Too many Required attributes on a single parameter ({method.Name} -> {methodParameter.Name}). It can only contain one!");

                // Test for the [Choice] Attribute
                // A parameter cna have multiple Choice attributes, and for each we're going to add it's key-value pair.
                foreach (Choice choice in methodParameter.GetCustomAttributes(typeof(Choice)))
                {
                    // If the parameter expects a string but the value of the choice is of type int, then throw an error.
                    if (newParameter.Type == ApplicationCommandOptionType.String)
                    {
                        if(String.IsNullOrEmpty(choice.choiceStringValue))
                        {
                            throw new Exception($"Parameter ({method.Name} -> {methodParameter.Name}) is of type string, but choice is of type int!");
                        }
                        newParameter.AddChoice(choice.choiceName, choice.choiceStringValue);
                    }
                    // If the parameter expects a int but the value of the choice is of type string, then throw an error.
                    if (newParameter.Type == ApplicationCommandOptionType.Integer)
                    {
                        if (choice.choiceIntValue == null)
                        {
                            throw new Exception($"Parameter ({method.Name} -> {methodParameter.Name}) is of type int, but choice is of type string!");
                        }
                        newParameter.AddChoice(choice.choiceName, (int)choice.choiceIntValue);
                    }
                }

                finalParameters.Add(newParameter);
            }
            return finalParameters;
        }
        /// <summary>
        /// Get the type of command option from a method parameter info.
        /// </summary>
        private static ApplicationCommandOptionType TypeFromMethodParameter(ParameterInfo methodParameter)
        {
            // Can't do switch -- who knows why?
            if (methodParameter.ParameterType == typeof(int) ||
                methodParameter.ParameterType == typeof(int?))
                return ApplicationCommandOptionType.Integer;
            if (methodParameter.ParameterType == typeof(string))
                return ApplicationCommandOptionType.String;
            if (methodParameter.ParameterType == typeof(bool) ||
                methodParameter.ParameterType == typeof(bool?))
                return ApplicationCommandOptionType.Boolean;
            if (methodParameter.ParameterType == typeof(SocketGuildChannel))
                return ApplicationCommandOptionType.Channel;
            if (methodParameter.ParameterType == typeof(SocketRole))
                return ApplicationCommandOptionType.Role;
            if (methodParameter.ParameterType == typeof(SocketGuildUser))
                return ApplicationCommandOptionType.User;

            throw new Exception($"Got parameter type other than int, string, bool, guild, role, or user. {methodParameter.Name}");
        }

        /// <summary>
        /// Gets whater the parameter can be set as null, in the case that parameter type usually does not allow null.
        /// More specifically tests to see if it is a type of 'int?' or 'bool?',
        /// </summary>
        private static bool GetNullableStatus(ParameterInfo methodParameter)
        {
            if(methodParameter.ParameterType == typeof(int?) ||
                methodParameter.ParameterType == typeof(bool?))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Creae a delegate from methodInfo. Taken from
        /// https://stackoverflow.com/a/40579063/8455128
        /// </summary>
        private static Delegate CreateDelegate(MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);

            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }

            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }

            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }

        public static async Task RegisterCommands(DiscordSocketClient socketClient, Dictionary<Type, SlashModuleInfo> rootModuleInfos, Dictionary<string, SlashCommandInfo> commandDefs, SlashCommandService slashCommandService, List<ulong> guildIDs,CommandRegistrationOptions options)
        {
            // TODO: see how we should handle if user wants to register two commands with the same name, one global and one not.
            // Build the commands
            List<SlashCommandCreationProperties> builtCommands = await BuildCommands(rootModuleInfos).ConfigureAwait(false);

            // Scan for each existing command on discord so we know what is already there.
            List<Rest.RestGuildCommand> existingGuildCommands = new List<Rest.RestGuildCommand>();
            List<Rest.RestGlobalCommand> existingGlobalCommands = new List<Rest.RestGlobalCommand>();
            existingGlobalCommands.AddRange(await socketClient.Rest.GetGlobalApplicationCommands().ConfigureAwait(false));
            foreach (ulong guildID in guildIDs)
            {
                existingGuildCommands.AddRange(await socketClient.Rest.GetGuildApplicationCommands(guildID).ConfigureAwait(false));
            }

            // If we want to keep the existing commands that are already registered
            // remove the commands that share the same name from the builtCommands list as to not overwrite.
            if (options.ExistingCommands == ExistingCommandOptions.KEEP_EXISTING)
            {
                foreach (var existingCommand in existingGuildCommands)
                {
                    builtCommands.RemoveAll(x => (!x.Global && x.Name == existingCommand.Name));
                }
                foreach (var existingCommand in existingGlobalCommands)
                {
                    builtCommands.RemoveAll(x => (x.Global && x.Name == existingCommand.Name));
                }
            }

            // If we want to delete commands that are not going to be re-implemented in builtCommands
            // or if we just want a blank slate
            if (options.OldCommands == OldCommandOptions.DELETE_UNUSED ||
                options.OldCommands == OldCommandOptions.WIPE)
            {                
                foreach (var existingCommand in existingGuildCommands)
                {
                    // If we want to wipe all GUILD commands
                    // or if the existing command isn't re-defined and re-built
                    // remove it from discord.
                    if (options.OldCommands == OldCommandOptions.WIPE ||
                        // There are no commands which contain this existing command.
                        !builtCommands.Any(x => !x.Global && x.Name.Contains(SlashModuleInfo.PathSeperator + existingCommand.Name)))
                    {
                        await existingCommand.DeleteAsync();
                    }
                }
                foreach (var existingCommand in existingGlobalCommands)
                {
                    // If we want to wipe all GLOBAL commands
                    // or if the existing command isn't re-defined and re-built
                    // remove it from discord.
                    if (options.OldCommands == OldCommandOptions.WIPE ||
                        // There are no commands which contain this existing command.
                        !builtCommands.Any(x => x.Global && x.Name.Contains(SlashModuleInfo.PathSeperator + existingCommand.Name)))
                    {
                        await existingCommand.DeleteAsync();
                    }
                }
            }

            // And now register them. Globally if the 'Global' flag is set.
            // If not then just register them as guild commands on all of the guilds given to us.
            foreach (var builtCommand in builtCommands)
            {
                if (builtCommand.Global)
                {
                    await socketClient.Rest.CreateGlobalCommand(builtCommand).ConfigureAwait(false);
                }
                else
                {
                    foreach (ulong guildID in guildIDs)
                    {
                        await socketClient.Rest.CreateGuildCommand(builtCommand, guildID).ConfigureAwait(false);
                    }
                }
            }

            return;
        }
        /// <summary>
        /// Build and return all of the commands this assembly contians.
        /// </summary>
        public static async Task<List<SlashCommandCreationProperties>> BuildCommands(Dictionary<Type, SlashModuleInfo> rootModuleInfos)
        {
            List<SlashCommandCreationProperties> builtCommands = new List<SlashCommandCreationProperties>();
            foreach (var pair in rootModuleInfos)
            {
                var rootModuleInfo = pair.Value;
                builtCommands.AddRange(rootModuleInfo.BuildCommands());
            }

            return builtCommands;
        }
    }
}
