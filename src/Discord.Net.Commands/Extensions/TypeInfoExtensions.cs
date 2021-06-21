using Discord.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord.Commands
{
    internal static class TypeInfoExtensions
    {
        public static async Task<bool> IsValidModuleType(this TypeInfo typeInfo, bool autoLoadable = false, Logger logger = null)
        {
            if(IsOuterSubTypeCandidate(typeInfo, out Type parentType))
            {
                return await IsValidOuterSubTypeAsync(typeInfo, parentType, autoLoadable, logger);
            }
            else
            {
                return await IsModuleAsync(typeInfo, logger, autoLoadable);
            }
        }

        public static bool IsOuterSubTypeCandidate(this TypeInfo typeInfo, out Type parentType)
        {
            var groupAttr = typeInfo.GetCustomAttribute<GroupAttribute>();
            if (groupAttr != null)
            {
                if (groupAttr.ParentModule != null)
                {
                    parentType = groupAttr.ParentModule;
                    return true;
                }
            }
            parentType = null;
            return false;
        }


        /// <param name="autoLoadable">If true, must not contain [DontAutoLoad] or will return false.</param>
        public static async Task<bool> IsValidOuterSubTypeAsync(this TypeInfo typeInfo, Type parentType, bool autoLoadable = false, Logger logger = null)
        {
            TypeInfo parentTypeInfo = parentType.GetTypeInfo();

            bool targetValid = await IsModuleAsync(typeInfo, logger, autoLoadable);
            if (targetValid)
            {
                bool parentValid = await IsModuleAsync(parentTypeInfo, logger, autoLoadable);
                if (!parentValid)
                {
                    if (logger != null)
                    {
                        await logger.WarningAsync($"Parent Class {parentTypeInfo.FullName} is not a module. Group module {typeInfo.FullName} was not loaded.").ConfigureAwait(false);
                    }
                    return false;
                }
            }
            return targetValid;
        }

        private static async Task<bool> IsModuleAsync(TypeInfo typeInfo, Logger logger, bool autoLoadable)
        {
            if(IsValidModuleDefinition(typeInfo))
            {
                if (autoLoadable)
                {
                    if (typeInfo.IsPublic || typeInfo.IsNestedPublic)
                    {
                        return !typeInfo.IsDefined(typeof(DontAutoLoadAttribute));
                    }
                    else if (IsAutoLoadableModuleCandidate(typeInfo))
                    {
                        if(logger != null)
                        {
                            await logger.WarningAsync($"Class {typeInfo.FullName} is not public and cannot be loaded. To suppress this message, mark the class with {nameof(DontAutoLoadAttribute)}.").ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsValidModuleDefinition(this TypeInfo typeInfo) =>
            ModuleClassBuilder.ModuleTypeInfo.IsAssignableFrom(typeInfo) &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.ContainsGenericParameters;

        private static bool IsAutoLoadableModuleCandidate(TypeInfo typeInfo) =>
            typeInfo.DeclaredMethods.Any(
                x => x.GetCustomAttribute<CommandAttribute>() != null) &&
                    typeInfo.GetCustomAttribute<DontAutoLoadAttribute>() == null;
    }
}
