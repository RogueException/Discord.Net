using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    public enum OldCommandOptions
    {
        /// <summary>
        /// Keep the old commands intact - do nothing to them.
        /// </summary>
        KEEP,
        /// <summary>
        /// Delete the old commands that we won't be re-defined this time around.
        /// </summary>
        DELETE_UNUSED,
        /// <summary>
        /// Delete everything discord has.
        /// </summary>
        WIPE
    }
}
