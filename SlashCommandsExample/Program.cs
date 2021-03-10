/*
 * This project, is at this moment used for testing and debugging the new and experimental Slash Commands.
 * After all testing has been done, and the project is ready to be integrated into the main Discord.Net ecosystem
 * this project should be re-made into one that could be used as an example usage of the new Slash Command Service.
 */
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.SlashCommands;
using Discord.WebSocket;

namespace SlashCommandsExample
{
	class Program
	{
        static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

            DiscordClient discordClient = new DiscordClient();
            // This could instead be handled in another thread, if for whatever reason you want to continue execution in the main Thread.
            discordClient.RunAsync().GetAwaiter().GetResult();
        }
    }
}
