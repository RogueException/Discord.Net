using Discord.Commands;
using Discord.Logging;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    public class SlashCommandService
    {
        // This semaphore is used to prevent race conditions.
        private readonly SemaphoreSlim _moduleLock;
        // This contains a dictionary of all definde SlashCommands, based on it's name
        public Dictionary<string, SlashCommandInfo> commandDefs;
        // This contains a list of all slash command modules defined by their user in their assembly.
        public Dictionary<Type, SlashModuleInfo> moduleDefs;

        // This is such a complicated method to log stuff...
        public event Func<LogMessage, Task> Log { add { _logEvent.Add(value); } remove { _logEvent.Remove(value); } }
        internal readonly AsyncEvent<Func<LogMessage, Task>> _logEvent = new AsyncEvent<Func<LogMessage, Task>>();
        internal Logger _logger;
        internal LogManager _logManager;

        public SlashCommandService() // TODO: possible config?
        {
            // max one thread
            _moduleLock = new SemaphoreSlim(1, 1);
            
            _logManager = new LogManager(LogSeverity.Info);
            _logManager.Message += async msg => await _logEvent.InvokeAsync(msg).ConfigureAwait(false);
            _logger = new Logger(_logManager, "SlshCommand");
        }

        /// <summary>
        /// Execute a slash command.
        /// </summary>
        /// <param name="interaction">Interaction data recieved from discord.</param>
        /// <returns></returns>
        public async Task<IResult> ExecuteAsync(SocketInteraction interaction)
        {
            SlashCommandInfo commandInfo;
            // Get the name of the actual command - be it a normal slash command or subcommand, and return the options we can give it.
            string name = GetSearchName(interaction.Data, out var resultingOptions);
            // We still need to make sure it is registerd.
            if (commandDefs.TryGetValue(name, out commandInfo))
            {
                // Then, set the context in which the command will be executed
                commandInfo.Module.userCommandModule.SetContext(interaction);
                // Then run the command and pass the interaction data over to the CommandInfo class
                return await commandInfo.ExecuteAsync(resultingOptions).ConfigureAwait(false);
            }
            else
            {
                return SearchResult.FromError(CommandError.UnknownCommand, $"There is no registered slash command with the name {interaction.Data.Name}");
            }
        }
        /// <summary>
        /// Get the name of the command we want to search for - be it a normal slash command or a sub command. Returns as out the options to be given to the method.
        /// /// </summary>
        /// <param name="interactionData"></param>
        /// <param name="resultingOptions"></param>
        /// <returns></returns>
        public string GetSearchName(SocketInteractionData interactionData, out IReadOnlyCollection<SocketInteractionDataOption> resultingOptions)
        {
            // The names are stored as such:
            //  TOP//top-level-command-name
            //  TOP//command-group//command-group//sub-command-name
            // What we are looking for is to get from the interaction the specific (sub)command and what we need to pass to the method.
            // So we start the search at TOP//{interactionData.name}
            // because we are going to go through each sub-option it has. If it is a subcommand/ command group then it's going to be
            // inside the dictionary as TOP//{interactionData.name}//{option.name}
            // If the option is a parameter we then know that we've reached the end of the call chain - this should be our coomand!
            string nameToSearch = SlashModuleInfo.RootCommandPrefix + interactionData.Name;
            var options = interactionData.Options;
            while(options != null && options.Count == 1)
            {
                string newName = nameToSearch + SlashModuleInfo.PathSeperator + GetFirstOption(options).Name;
                if (AnyKeyContains(commandDefs,newName))
                {
                    nameToSearch = newName;
                    options = GetFirstOption(options).Options;
                }
                else
                {
                    break;
                }
            }
            resultingOptions = options;
            return nameToSearch;
        }
        /// <summary>
        /// Test to see if any <b>string</b> key contains another string inside it.
        /// </summary>
        private bool AnyKeyContains(Dictionary<string, SlashCommandInfo> commandDefs, string newName)
        {
            foreach (var pair in commandDefs)
            {
                if (pair.Key.Contains(newName))
                    return true;
            }
            return false;
        }

        private SocketInteractionDataOption GetFirstOption(IReadOnlyCollection<SocketInteractionDataOption> options)
        {
            var it = options.GetEnumerator();
            it.MoveNext();
            return it.Current;
        }

        /// <summary>
        /// Registers with discord all previously scanned commands.
        /// </summary>
        public async Task RegisterCommandsAsync(DiscordSocketClient socketClient, List<ulong> guildIDs, CommandRegistrationOptions registrationOptions)
        {
            // First take a hold of the module lock, as to make sure we aren't editing stuff while we do our business
            await _moduleLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Build and register all of the commands.
                await SlashCommandServiceHelper.RegisterCommands(socketClient, moduleDefs, commandDefs, this, guildIDs, registrationOptions).ConfigureAwait(false);
            }
            finally
            {
                _moduleLock.Release();
            }
            await _logger.InfoAsync("All commands have been registered!").ConfigureAwait(false);
        }

        /// <summary>
        /// Build all the commands and return them, for manual registration with Discord. This is automatically done in <see cref="RegisterCommandsAsync(DiscordSocketClient, List{ulong}, CommandRegistrationOptions)"/>
        /// </summary>
        /// <returns>A list of all the valid commands found within this Assembly.</returns>
        public async Task<List<SlashCommandCreationProperties>> BuildCommands()
        {
            // First take a hold of the module lock, as to make sure we aren't editing stuff while we do our business
            await _moduleLock.WaitAsync().ConfigureAwait(false);
            List<SlashCommandCreationProperties> result;
            try
            {
                result = await SlashCommandServiceHelper.BuildCommands(moduleDefs).ConfigureAwait(false);
            }
            finally
            {
                _moduleLock.Release();
            }
            await _logger.InfoAsync("All commands have been built!").ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Scans the program for Attribute-based SlashCommandModules
        /// </summary>
        public async Task AddModulesAsync(Assembly assembly, IServiceProvider services)
        {
            // First take a hold of the module lock, as to make sure we aren't editing stuff while we do our business
            await _moduleLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Get all of the modules that were properly defined by the user.
                IReadOnlyList<TypeInfo> types = await SlashCommandServiceHelper.GetValidModuleClasses(assembly, this).ConfigureAwait(false);
                // Then, based on that, make an instance out of each of them, and get the resulting SlashModuleInfo s
                moduleDefs = await SlashCommandServiceHelper.InstantiateModules(types, this).ConfigureAwait(false);
                // After that, internally register all of the commands into SlashCommandInfo
                commandDefs = await SlashCommandServiceHelper.CreateCommandInfos(types,moduleDefs,this).ConfigureAwait(false);
            }
            finally
            {
                _moduleLock.Release();
            }
        }
    }
}
