using Discord.Commands.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{

    public interface ISlashCommandModule
    {
        void SetContext(IDiscordInteraction interaction);

        //void BeforeExecute(CommandInfo command);

        //void AfterExecute(CommandInfo command);

        //void OnModuleBuilding(CommandService commandService, ModuleBuilder builder);
    }
}
