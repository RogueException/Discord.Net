using Discord;
using Discord.Commands;
using Discord.SlashCommands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace SlashCommandsExample
{
    class DiscordClient
    {
        public static DiscordSocketClient socketClient { get; set; } = new DiscordSocketClient();
        public static SlashCommandService _commands { get; set; }
        public static IServiceProvider _services { get; set; }

        private string botToken = "<YOUR TOKEN HERE>";

        public DiscordClient()
        {
            _commands = new SlashCommandService();
            _services = new ServiceCollection()
                .AddSingleton(socketClient)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            socketClient.Log += SocketClient_Log;
            _commands.Log += SocketClient_Log;
            socketClient.InteractionCreated += InteractionHandler;
            socketClient.Ready += RegisterCommand;

            // This is for dev purposes.
            // To avoid the situation in which you accidentally push your bot token to upstream, you can use
            // EnviromentVariables to store your key. 
            botToken = Environment.GetEnvironmentVariable("DiscordSlashCommandsBotToken", EnvironmentVariableTarget.User);
            // Uncomment the next line of code to set the environment variable.
            //  ------------------------------------------------------------------
            // | WARNING!                                                         |
            // |                                                                  |
            // | MAKE SURE TO DELETE YOUR TOKEN AFTER YOU HAVE SET THE VARIABLE   |
            // |                                                                  |
            //  ------------------------------------------------------------------

            //Environment.SetEnvironmentVariable("DiscordSlashCommandsBotToken",
            //    "[YOUR TOKEN GOES HERE    DELETE & COMMENT AFTER USE]",
            //    EnvironmentVariableTarget.User);
        }

        public async Task RegisterCommand()
        {
            // Use this to manually register a command for testing.
            return;
            await socketClient.Rest.CreateGuildCommand(new SlashCommandCreationProperties()
            {
                Name = "root",
                Description = "Root Command",
                Options = new List<ApplicationCommandOptionProperties>()
                {
                    new ApplicationCommandOptionProperties()
                    {
                        Name = "usr",
                        Description = "User Folder",
                        Type = ApplicationCommandOptionType.SubCommandGroup,
                        Options = new List<ApplicationCommandOptionProperties>()
                        {
                            // This doesn't work. This is good!
                            //new ApplicationCommandOptionProperties()
                            //{
                            //    Name = "strstr",
                            //    Description = "Some random string I guess.",
                            //    Type = ApplicationCommandOptionType.String,
                            //},
                            new ApplicationCommandOptionProperties()
                            {
                                Name = "zero",
                                Description = "Zero's Home Folder - COMMAND",
                                Type = ApplicationCommandOptionType.SubCommand,
                                Options = new List<ApplicationCommandOptionProperties>()
                                {
                                    new ApplicationCommandOptionProperties()
                                    {
                                        Name = "file",
                                        Description = "the file you want accessed.",
                                        Type = ApplicationCommandOptionType.String
                                    }
                                }
                            },
                            new ApplicationCommandOptionProperties()
                            {
                                Name = "johhny",
                                Description = "Johnny Test's Home Folder - COMMAND",
                                Type = ApplicationCommandOptionType.SubCommand,
                                Options = new List<ApplicationCommandOptionProperties>()
                                {
                                    new ApplicationCommandOptionProperties()
                                    {
                                        Name = "file",
                                        Description = "the file you want accessed.",
                                        Type = ApplicationCommandOptionType.String
                                    }
                                }
                            }
                        }
                    },
                     new ApplicationCommandOptionProperties()
                     {
                        Name = "random",
                        Description = "Random things",
                        Type = ApplicationCommandOptionType.SubCommand
                     }
                }
            }, 386658607338618891) ;
        }

        public async Task RunAsync()
        {
            await socketClient.LoginAsync(TokenType.Bot, botToken);
            await socketClient.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _commands.RegisterCommandsAsync(socketClient, new List<ulong>()
            {
                386658607338618891
            },
            new CommandRegistrationOptions(OldCommandOptions.DELETE_UNUSED,ExistingCommandOptions.OVERWRITE));

            // If you would like to register your commands manually use:
            //-----------------------------------------//
            //
            // await _commands.BuildCommands();
            //
            //-----------------------------------------//
            // Though I wouldn't highly recommend it unless you want to do something very specific with them
            // such as only registering some commands on only some guilds, or editing them manually.

            await Task.Delay(-1);
        }

        private async Task InteractionHandler(SocketInteraction arg)
        {
            if(arg.Type == InteractionType.ApplicationCommand)
            {
                await _commands.ExecuteAsync(arg);
            }
        }

        private Task SocketClient_Log(LogMessage arg)
        {
            Console.WriteLine("[Discord] " + arg.ToString());
            return Task.CompletedTask;
        }
    }
}
