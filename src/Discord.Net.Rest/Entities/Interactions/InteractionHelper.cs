using Discord.API;
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Rest
{
    internal static class InteractionHelper
    {
        internal static async Task<RestUserMessage> SendFollowupAsync(BaseDiscordClient client, API.Rest.CreateWebhookMessageParams args,
            string token, IMessageChannel channel, RequestOptions options = null)
        {
            var model = await client.ApiClient.CreateInteractionFollowupMessage(args, token, options).ConfigureAwait(false);

            var entity = RestUserMessage.Create(client, channel, client.CurrentUser, model);
            return entity;
        }
        
        // Global commands
        internal static async Task<RestGlobalCommand> CreateGlobalCommand(BaseDiscordClient client,
            Action<SlashCommandCreationProperties> func, RequestOptions options = null)
        {
            var args = new SlashCommandCreationProperties();
            func(args);
            return await CreateGlobalCommand(client, args, options).ConfigureAwait(false);
        }
        internal static async Task<RestGlobalCommand> CreateGlobalCommand(BaseDiscordClient client,
            SlashCommandCreationProperties args, RequestOptions options = null)
        {
            if (args.Options.IsSpecified)
            {
                if (args.Options.Value.Count > 10)
                    throw new ArgumentException("Option count must be 10 or less");
            }

            

            var model = new CreateApplicationCommandParams()
            {
                Name = args.Name,
                Description = args.Description,
                Options = args.Options.IsSpecified
                    ? args.Options.Value.Select(x => new Discord.API.ApplicationCommandOption(x)).ToArray()
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified
            };

            var cmd = await client.ApiClient.CreateGlobalApplicationCommandAsync(model, options).ConfigureAwait(false);
            return RestGlobalCommand.Create(client, cmd);
        }
        internal static async Task<RestGlobalCommand> ModifyGlobalCommand(BaseDiscordClient client, RestGlobalCommand command,
           Action<ApplicationCommandProperties> func, RequestOptions options = null)
        {
            ApplicationCommandProperties args = new ApplicationCommandProperties();
            func(args);

            if (args.Options.IsSpecified)
            {
                if (args.Options.Value.Count > 10)
                    throw new ArgumentException("Option count must be 10 or less");
            }

            var model = new Discord.API.Rest.ModifyApplicationCommandParams()
            {
                Name = args.Name,
                Description = args.Description,
                Options = args.Options.IsSpecified
                    ? args.Options.Value.Select(x => new Discord.API.ApplicationCommandOption(x)).ToArray()
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified
            };

            var msg = await client.ApiClient.ModifyGlobalApplicationCommandAsync(model, command.Id, options).ConfigureAwait(false);
            command.Update(msg);
            return command;
        }


        internal static async Task DeleteGlobalCommand(BaseDiscordClient client, RestGlobalCommand command, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));
            Preconditions.NotEqual(command.Id, 0, nameof(command.Id));

            await client.ApiClient.DeleteGlobalApplicationCommandAsync(command.Id, options).ConfigureAwait(false);
        }

        // Guild Commands
        internal static async Task<RestGuildCommand> CreateGuildCommand(BaseDiscordClient client, ulong guildId,
            Action<SlashCommandCreationProperties> func, RequestOptions options = null)
        {
            var args = new SlashCommandCreationProperties();
            func(args);

            return await CreateGuildCommand(client, guildId, args, options).ConfigureAwait(false);
        }
        internal static async Task<RestGuildCommand> CreateGuildCommand(BaseDiscordClient client, ulong guildId,
           SlashCommandCreationProperties args, RequestOptions options = null)
        {
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            Preconditions.NotNullOrEmpty(args.Description, nameof(args.Description));


            if (args.Options.IsSpecified)
            {
                if (args.Options.Value.Count > 10)
                    throw new ArgumentException("Option count must be 10 or less");

                foreach(var item in args.Options.Value)
                {
                    Preconditions.NotNullOrEmpty(item.Name, nameof(item.Name));
                    Preconditions.NotNullOrEmpty(item.Description, nameof(item.Description));
                }
            }

            var model = new CreateApplicationCommandParams()
            {
                Name = args.Name,
                Description = args.Description,
                Options = args.Options.IsSpecified
                    ? args.Options.Value.Select(x => new Discord.API.ApplicationCommandOption(x)).ToArray()
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified
            };

            var cmd = await client.ApiClient.CreateGuildApplicationCommandAsync(model, guildId, options).ConfigureAwait(false);
            return RestGuildCommand.Create(client, cmd, guildId);
        }
        internal static async Task<RestGuildCommand> ModifyGuildCommand(BaseDiscordClient client, RestGuildCommand command,
           Action<ApplicationCommandProperties> func, RequestOptions options = null)
        {
            ApplicationCommandProperties args = new ApplicationCommandProperties();
            func(args);

            if (args.Options.IsSpecified)
            {
                if (args.Options.Value.Count > 10)
                    throw new ArgumentException("Option count must be 10 or less");
            }

            var model = new Discord.API.Rest.ModifyApplicationCommandParams()
            {
                Name = args.Name,
                Description = args.Description,
                Options = args.Options.IsSpecified
                    ? args.Options.Value.Select(x => new Discord.API.ApplicationCommandOption(x)).ToArray()
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified
            };

            var msg = await client.ApiClient.ModifyGuildApplicationCommandAsync(model, command.GuildId, command.Id, options).ConfigureAwait(false);
            command.Update(msg);
            return command;
        }

        internal static async Task DeleteGuildCommand(BaseDiscordClient client, RestGuildCommand command, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));
            Preconditions.NotEqual(command.Id, 0, nameof(command.Id));

            await client.ApiClient.DeleteGuildApplicationCommandAsync(command.GuildId, command.Id, options).ConfigureAwait(false);
        }
    }
}
