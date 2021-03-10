using Discord.SlashCommands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlashCommandsExample.Modules
{
    // Doesn't inherit from SlashCommandModule
    public class InvalidDefinition : Object
    {
        // commands
    }

    // Isn't public
    class PrivateDefinition : SlashCommandModule<SocketInteraction>
    {
        // commands
    }
}
