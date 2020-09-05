using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Logging;
using Discord.Rest;

namespace Discord.Webhook
{
    /// <summary> A client responsible for connecting as a Webhook. </summary>
    public class DiscordWebhookClient : IDisposable
    {
        public event Func<LogMessage, Task> Log { add { _logEvent.Add(value); } remove { _logEvent.Remove(value); } }
        internal readonly AsyncEvent<Func<LogMessage, Task>> _logEvent = new AsyncEvent<Func<LogMessage, Task>>();

        private readonly ulong _webhookId;
        internal IWebhook Webhook;
        internal readonly Logger _restLogger;
        
        internal API.DiscordRestApiClient ApiClient { get; }
        internal LogManager LogManager { get; }

        /// <summary> Creates a new Webhook Discord client. </summary>
        public DiscordWebhookClient(IWebhook webhook)
            : this(webhook.Id, webhook.Token, new DiscordRestConfig()) {  }
        /// <summary> Creates a new Webhook Discord client. </summary>
        public DiscordWebhookClient(ulong webhookId, string webhookToken)
            : this(webhookId, webhookToken, new DiscordRestConfig()) { }
        /// <summary> Creates a new Webhook Discord client. </summary>
        public DiscordWebhookClient(string webhookUrl)
            : this(webhookUrl, new DiscordRestConfig()) { }

        // regex pattern to match webhook urls
        private static Regex WebhookUrlRegex = new Regex(@"^.*(discord|discordapp)\.com\/api\/webhooks\/([\d]+)\/([a-z0-9_-]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary> Creates a new Webhook Discord client. </summary>
        public DiscordWebhookClient(ulong webhookId, string webhookToken, DiscordRestConfig config)
            : this(config)
        {
            _webhookId = webhookId;
            ApiClient.LoginAsync(TokenType.Webhook, webhookToken).GetAwaiter().GetResult();
            Webhook = WebhookClientHelper.GetWebhookAsync(this, webhookId).GetAwaiter().GetResult();
        }
        /// <summary> Creates a new Webhook Discord client. </summary>
        public DiscordWebhookClient(IWebhook webhook, DiscordRestConfig config)
            : this(config)
        {
            Webhook = webhook;
            _webhookId = Webhook.Id;
        }

        /// <summary>
        ///     Creates a new Webhook Discord client.
        /// </summary>
        /// <param name="webhookUrl">The url of the webhook.</param>
        /// <param name="config">The configuration options to use for this client.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="webhookUrl"/> is an invalid format.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="webhookUrl"/> is null or whitespace.</exception>
        public DiscordWebhookClient(string webhookUrl, DiscordRestConfig config) : this(config)
        {
            string token;
            ParseWebhookUrl(webhookUrl, out _webhookId, out token);
            ApiClient.LoginAsync(TokenType.Webhook, token).GetAwaiter().GetResult();
            Webhook = WebhookClientHelper.GetWebhookAsync(this, _webhookId).GetAwaiter().GetResult();
        }

        private DiscordWebhookClient(DiscordRestConfig config)
        {
            ApiClient = CreateApiClient(config);
            LogManager = new LogManager(config.LogLevel);
            LogManager.Message += async msg => await _logEvent.InvokeAsync(msg).ConfigureAwait(false);

            _restLogger = LogManager.CreateLogger("Rest");

            ApiClient.RequestQueue.RateLimitTriggered += async (id, info) =>
            {
                if (info == null)
                    await _restLogger.VerboseAsync($"Preemptive Rate limit triggered: {id?.ToString() ?? "null"}").ConfigureAwait(false);
                else
                    await _restLogger.WarningAsync($"Rate limit triggered: {id?.ToString() ?? "null"}").ConfigureAwait(false);
            };
            ApiClient.SentRequest += async (method, endpoint, millis) => await _restLogger.VerboseAsync($"{method} {endpoint}: {millis} ms").ConfigureAwait(false);
        }
        private static API.DiscordRestApiClient CreateApiClient(DiscordRestConfig config)
            => new API.DiscordRestApiClient(config.RestClientProvider, DiscordRestConfig.UserAgent);

        /// <summary>
        ///     Sends a message to the channel for this webhook.
        /// </summary>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embeds">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/>s to be sent.</param>
        /// <param name="username">The username the webhook will display on this message.</param>
        /// <param name="avatarUrl">The url to the avatar the webhook will display on this message.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Returns the ID of the created message.
        /// </returns>
        public Task<ulong> SendMessageAsync(string text = null, bool isTTS = false, IEnumerable<Embed> embeds = null,
            string username = null, string avatarUrl = null, RequestOptions options = null)
            => WebhookClientHelper.SendMessageAsync(this, text, isTTS, embeds, username, avatarUrl, options);

        /// <summary>
        ///     Sends a message to the channel for this webhook with an attachment.
        /// </summary>
        /// <param name="filePath">The file path of the file.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embeds">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/>s to be sent.</param>
        /// <param name="username">The username the webhook will display on this message.</param>
        /// <param name="avatarUrl">The url to the avatar the webhook will display on this message.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Returns the ID of the created message.
        /// </returns>
        public Task<ulong> SendFileAsync(string filePath, string text, bool isTTS = false,
            IEnumerable<Embed> embeds = null, string username = null, string avatarUrl = null, RequestOptions options = null, bool isSpoiler = false)
            => WebhookClientHelper.SendFileAsync(this, filePath, text, isTTS, embeds, username, avatarUrl, options, isSpoiler);

        /// <summary>
        ///     Sends a message to the channel for this webhook with an attachment.
        /// </summary>
        /// <param name="stream">The stream of the file to be sent.</param>
        /// <param name="filename">The name of the file to be sent.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embeds">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/>s to be sent.</param>
        /// <param name="username">The username the webhook will display on this message.</param>
        /// <param name="avatarUrl">The url to the avatar the webhook will display on this message.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Returns the ID of the created message.
        /// </returns>
        public Task<ulong> SendFileAsync(Stream stream, string filename, string text, bool isTTS = false,
            IEnumerable<Embed> embeds = null, string username = null, string avatarUrl = null, RequestOptions options = null, bool isSpoiler = false)
            => WebhookClientHelper.SendFileAsync(this, stream, filename, text, isTTS, embeds, username, avatarUrl, options, isSpoiler);

        /// <summary>
        ///     Sends a message to the channel for this webhook with multiple attachments.
        /// </summary>
        /// <param name="filePaths">The <see cref="IEnumerable{T}"/> of file paths of the files.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embeds">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/>s to be sent.</param>
        /// <param name="username">The username the webhook will display on this message.</param>
        /// <param name="avatarUrl">The url to the avatar the webhook will display on this message.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Returns the ID of the created message.
        /// </returns>
        public Task<ulong> SendFileAsync(IEnumerable<string> filePaths, string text, bool isTTS = false,
            IEnumerable<Embed> embeds = null, string username = null, string avatarUrl = null, RequestOptions options = null, bool isSpoiler = false)
            => WebhookClientHelper.SendFileAsync(this, filePaths, text, isTTS, embeds, username, avatarUrl, options, isSpoiler);

        /// <summary>
        ///     Sends a message to the channel for this webhook with multiple attachments.
        /// </summary>
        /// <param name="streams">The <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>
        /// that are the filename and stream for each file.</param>
        /// <param name="text">The message to be sent.</param>
        /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
        /// <param name="embeds">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/>s to be sent.</param>
        /// <param name="username">The username the webhook will display on this message.</param>
        /// <param name="avatarUrl">The url to the avatar the webhook will display on this message.</param>
        /// <param name="isSpoiler">Whether the message attachment should be hidden as a spoiler.</param>
        /// <param name="options">The options to be used when sending the request.</param>
        /// <returns>
        ///     Returns the ID of the created message.
        /// </returns>
        public Task<ulong> SendFileAsync(IEnumerable<KeyValuePair<string, Stream>> streams, string text, bool isTTS = false,
            IEnumerable<Embed> embeds = null, string username = null, string avatarUrl = null, RequestOptions options = null, bool isSpoiler = false)
            => WebhookClientHelper.SendFileAsync(this, streams, text, isTTS, embeds, username, avatarUrl, options, isSpoiler);

        /// <summary> Modifies the properties of this webhook. </summary>
        public Task ModifyWebhookAsync(Action<WebhookProperties> func, RequestOptions options = null)
            => Webhook.ModifyAsync(func, options);

        /// <summary> Deletes this webhook from Discord and disposes the client. </summary>
        public async Task DeleteWebhookAsync(RequestOptions options = null)
        {
            await Webhook.DeleteAsync(options).ConfigureAwait(false);
            Dispose();
        }

        public void Dispose()
        {
            ApiClient?.Dispose();
        }

        internal static void ParseWebhookUrl(string webhookUrl, out ulong webhookId, out string webhookToken)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(paramName: nameof(webhookUrl), message:
                    "The given webhook Url cannot be null or whitespace.");

            // thrown when groups are not populated/valid, or when there is no match
            ArgumentException ex(string reason = null)
                => new ArgumentException(paramName: nameof(webhookUrl), message:
                $"The given webhook Url was not in a valid format. {reason}");
            var match = WebhookUrlRegex.Match(webhookUrl);
            if (match != null)
            {
                // ensure that the first group is a ulong, set the _webhookId
                // 0th group is always the entire match, and 1 is the domain; so start at index 2
                if (!(match.Groups[2].Success && ulong.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out webhookId)))
                    throw ex("The webhook Id could not be parsed.");

                if (!match.Groups[3].Success)
                    throw ex("The webhook token could not be parsed.");
                webhookToken = match.Groups[3].Value;
            }
            else
                throw ex();
        }
    }
}
