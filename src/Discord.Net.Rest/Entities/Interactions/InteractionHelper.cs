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
        internal static async Task<RestInteractionMessage> SendInteractionResponse(BaseDiscordClient client, IMessageChannel channel, InteractionResponse response,
            ulong interactionId, string interactionToken, RequestOptions options = null)
        {
            await client.ApiClient.CreateInteractionResponse(response, interactionId, interactionToken, options).ConfigureAwait(false);

            // get the original message
            var msg = await client.ApiClient.GetInteractionResponse(interactionToken).ConfigureAwait(false);

            var entity = RestInteractionMessage.Create(client, msg, interactionToken, channel);

            return entity;
        }

        internal static async Task<RestFollowupMessage> SendFollowupAsync(BaseDiscordClient client, API.Rest.CreateWebhookMessageParams args,
            string token, IMessageChannel channel, RequestOptions options = null)
        {
            var model = await client.ApiClient.CreateInteractionFollowupMessage(args, token, options).ConfigureAwait(false);

            RestFollowupMessage entity = RestFollowupMessage.Create(client, model, token, channel);
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
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified,
                DefaultPermission = args.DefaultPermission.IsSpecified
                    ? args.DefaultPermission.Value
                    : Optional<bool>.Unspecified
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
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified,
                DefaultPermission = args.DefaultPermission.IsSpecified
                    ? args.DefaultPermission.Value
                    : Optional<bool>.Unspecified
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
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified,
                DefaultPermission = args.DefaultPermission.IsSpecified
                    ? args.DefaultPermission.Value
                    : Optional<bool>.Unspecified
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
                    : Optional<Discord.API.ApplicationCommandOption[]>.Unspecified,
                DefaultPermission = args.DefaultPermission.IsSpecified
                    ? args.DefaultPermission.Value
                    : Optional<bool>.Unspecified
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

        internal static async Task<Discord.API.Message> ModifyFollowupMessage(BaseDiscordClient client, RestFollowupMessage message, Action<MessageProperties> func,
            RequestOptions options = null)
        {
            var args = new MessageProperties();
            func(args);

            bool hasText = args.Content.IsSpecified ? !string.IsNullOrEmpty(args.Content.Value) : !string.IsNullOrEmpty(message.Content);
            bool hasEmbed = args.Embed.IsSpecified ? args.Embed.Value != null : message.Embeds.Any();
            if (!hasText && !hasEmbed)
                Preconditions.NotNullOrEmpty(args.Content.IsSpecified ? args.Content.Value : string.Empty, nameof(args.Content));

            var apiArgs = new API.Rest.ModifyInteractionResponseParams
            {
                Content = args.Content,
                Embeds = args.Embed.IsSpecified ? new API.Embed[] { args.Embed.Value.ToModel() } : Optional.Create<API.Embed[]>()
            };

            return await client.ApiClient.ModifyInteractionFollowupMessage(apiArgs, message.Id, message.Token, options).ConfigureAwait(false);
        }

        internal static async Task DeleteFollowupMessage(BaseDiscordClient client, RestFollowupMessage message, RequestOptions options = null)
            => await client.ApiClient.DeleteInteractionFollowupMessage(message.Id, message.Token, options);

        internal static async Task<Discord.API.Message> ModifyInteractionResponse(BaseDiscordClient client, RestInteractionMessage message, Action<MessageProperties> func,
           RequestOptions options = null)
        {
            var args = new MessageProperties();
            func(args);

            bool hasText = args.Content.IsSpecified ? !string.IsNullOrEmpty(args.Content.Value) : !string.IsNullOrEmpty(message.Content);
            bool hasEmbed = args.Embed.IsSpecified ? args.Embed.Value != null : message.Embeds.Any();
            if (!hasText && !hasEmbed)
                Preconditions.NotNullOrEmpty(args.Content.IsSpecified ? args.Content.Value : string.Empty, nameof(args.Content));

            var apiArgs = new API.Rest.ModifyInteractionResponseParams
            {
                Content = args.Content,
                Embeds = args.Embed.IsSpecified ? new API.Embed[] { args.Embed.Value.ToModel() } : Optional.Create<API.Embed[]>()
            };

            return await client.ApiClient.ModifyInteractionFollowupMessage(apiArgs, message.Id, message.Token, options).ConfigureAwait(false);
        }

        internal static async Task DeletedInteractionResponse(BaseDiscordClient client, RestInteractionMessage message, RequestOptions options = null)
            => await client.ApiClient.DeleteInteractionFollowupMessage(message.Id, message.Token, options);

        // Guild permissions
        internal static async Task<IReadOnlyCollection<Discord.GuildApplicationCommandPermissions>> GetCommandGuildPermissions(BaseDiscordClient client,
            RestGuildCommand command)
        {
            // TODO
            return null;
        }

    }
}
