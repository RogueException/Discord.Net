using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord.Commands
{
    internal sealed class NamedArgumentTypeReader<T> : TypeReader
        where T : class, new()
    {
        private static readonly IReadOnlyDictionary<string, PropertyInfo> _tProps = typeof(T).GetTypeInfo().DeclaredProperties
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic && !p.SetMethod.IsStatic)
            .ToImmutableDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        private readonly CommandService _commands;

        public NamedArgumentTypeReader(CommandService commands)
        {
            _commands = commands;
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var result = new T();
            var state = ReadState.LookingForParameter;
            int beginRead = 0, currentRead = 0;

            while (state != ReadState.End)
            {
                try
                {
                    var prop = Read(out var arg);
                    var propVal = await ReadArgumentAsync(prop, arg).ConfigureAwait(false);
                    if (propVal != null)
                        prop.SetMethod.Invoke(result, new[] { propVal });
                    else
                        return TypeReaderResult.FromError(CommandError.ParseFailed, $"Could not parse the argument for the parameter '{prop.Name}' as type '{prop.PropertyType}'.");
                }
                catch (Exception ex)
                {
                    return TypeReaderResult.FromError(ex);
                }
            }

            return TypeReaderResult.FromSuccess(result);

            PropertyInfo Read(out string arg)
            {
                string currentParam = null;
                char match = '\0';

                for (; currentRead < input.Length; currentRead++)
                {
                    var currentChar = input[currentRead];
                    switch (state)
                    {
                        case ReadState.LookingForParameter:
                            if (Char.IsWhiteSpace(currentChar))
                                continue;
                            else
                            {
                                beginRead = currentRead;
                                state = ReadState.InParameter;
                            }
                            break;
                        case ReadState.InParameter:
                            if (currentChar != ':')
                                continue;
                            else
                            {
                                currentParam = input.Substring(beginRead, currentRead - beginRead);
                                state = ReadState.LookingForArgument;
                            }
                            break;
                        case ReadState.LookingForArgument:
                            if (Char.IsWhiteSpace(currentChar))
                                continue;
                            else
                            {
                                beginRead = currentRead;
                                state = (QuotationAliasUtils.GetDefaultAliasMap.TryGetValue(currentChar, out match))
                                    ? ReadState.InQuotedArgument
                                    : ReadState.InArgument;
                            }
                            break;
                        case ReadState.InArgument:
                            if (!Char.IsWhiteSpace(currentChar))
                                continue;
                            else
                                return GetPropAndValue(out arg);
                        case ReadState.InQuotedArgument:
                            if (currentChar != match)
                                continue;
                            else
                                return GetPropAndValue(out arg);
                    }
                }

                if (currentParam == null)
                    throw new InvalidOperationException("No parameter name was read.");

                return GetPropAndValue(out arg);

                PropertyInfo GetPropAndValue(out string argv)
                {
                    bool quoted = state == ReadState.InQuotedArgument;
                    state = (currentRead == (quoted ? input.Length - 1 : input.Length))
                        ? ReadState.End
                        : ReadState.LookingForParameter;

                    if (quoted)
                    {
                        argv = input.Substring(beginRead + 1, currentRead - beginRead - 1).Trim();
                        currentRead++;
                    }
                    else 
                        argv = input.Substring(beginRead, currentRead - beginRead);

                    return _tProps[currentParam];
                }
            }

            async Task<object> ReadArgumentAsync(PropertyInfo prop, string arg)
            {
                var elemType = prop.PropertyType;
                bool isCollection = false;
                if (elemType.GetTypeInfo().IsGenericType && elemType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elemType = prop.PropertyType.GenericTypeArguments[0];
                    isCollection = true;
                }

                var overridden = prop.GetCustomAttribute<OverrideTypeReaderAttribute>();
                var reader = (overridden != null)
                    ? ModuleClassBuilder.GetTypeReader(_commands, elemType, overridden.TypeReader, services)
                    : (_commands.GetTypeReaders(elemType, false)?.FirstOrDefault().Value
                        ?? _commands.GetDefaultTypeReader(elemType));

                if (reader != null)
                {
                    if (isCollection)
                    {
                        var method = _readMultipleMethod.MakeGenericMethod(elemType);
                        var task = (Task<IEnumerable>)method.Invoke(null, new object[] { reader, context, arg.Split(','), services });
                        return await task.ConfigureAwait(false);
                    }
                    else
                        return await ReadSingle(reader, context, arg, services).ConfigureAwait(false);
                }
                return null;
            }
        }

        private static async Task<object> ReadSingle(TypeReader reader, ICommandContext context, string arg, IServiceProvider services)
        {
            var readResult = await reader.ReadAsync(context, arg, services).ConfigureAwait(false);
            return (readResult.IsSuccess)
                ? readResult.BestMatch
                : null;
        }
        private static async Task<IEnumerable> ReadMultiple<TObj>(TypeReader reader, ICommandContext context, IEnumerable<string> args, IServiceProvider services)
        {
            var objs = new List<TObj>();
            foreach (var arg in args)
            {
                var read = await ReadSingle(reader, context, arg.Trim(), services).ConfigureAwait(false);
                if (read != null)
                    objs.Add((TObj)read);
            }
            return objs.ToImmutableArray();
        }
        private static readonly MethodInfo _readMultipleMethod = typeof(NamedArgumentTypeReader<T>)
            .GetTypeInfo()
            .DeclaredMethods
            .Single(m => m.IsPrivate && m.IsStatic && m.Name == nameof(ReadMultiple));

        private enum ReadState
        {
            LookingForParameter,
            InParameter,
            LookingForArgument,
            InArgument,
            InQuotedArgument,
            End
        }
    }
}
