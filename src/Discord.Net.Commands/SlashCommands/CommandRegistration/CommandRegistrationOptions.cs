using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    /// The options that should be kept in mind when registering the slash commands to discord.
    /// </summary>
    public class CommandRegistrationOptions
    {
        /// <summary>
        /// The options that should be kept in mind when registering the slash commands to discord.
        /// </summary>
        /// <param name="oldCommands">What to do with the old commands that are already registered with discord</param>
        /// <param name="existingCommands"> What to do with the old commands (if they weren't wiped) that we re-define.</param>
        public CommandRegistrationOptions(OldCommandOptions oldCommands, ExistingCommandOptions existingCommands)
        {
            OldCommands = oldCommands;
            ExistingCommands = existingCommands;
        }
        /// <summary>
        /// What to do with the old commands that are already registered with discord
        /// </summary>
        public OldCommandOptions OldCommands { get; set; }
        /// <summary>
        /// What to do with the old commands (if they weren't wiped) that we re-define.
        /// </summary>
        public ExistingCommandOptions ExistingCommands { get; set; }

        /// <summary>
        /// The default, and reccomended options - Keep the old commands, and overwrite existing commands we re-defined.
        /// </summary>
        public static CommandRegistrationOptions Default =>
            new CommandRegistrationOptions(OldCommandOptions.KEEP, ExistingCommandOptions.OVERWRITE);
    }
}
