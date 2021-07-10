using System;
using Discord.Commands.Builders;

namespace Discord.Commands
{
    internal interface IModuleBase
    {
        void SetContext(ICommandContext context);

        void BeforeExecute(CommandInfo command);
        
        void AfterExecute(CommandInfo command);

        void OnException(CommandInfo command, Exception exception);

        void OnModuleBuilding(CommandService commandService, ModuleBuilder builder);
    }
}
