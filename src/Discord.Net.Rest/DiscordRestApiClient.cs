
#pragma warning disable CS1591
using Discord.API.Rest;
using Discord.Net;
using Discord.Net.Converters;
using Discord.Net.Queue;
using Discord.Net.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.API
{
    internal class DiscordRestApiClient : IDisposable
    {
        private static readonly ConcurrentDictionary<string, Func<BucketIds, BucketId>> _bucketIdGenerators = new ConcurrentDictionary<string, Func<BucketIds, BucketId>>();

        public event Func<string, string, double, Task> SentRequest { add { _sentRequestEvent.Add(value); } remove { _sentRequestEvent.Remove(value); } }
        private readonly AsyncEvent<Func<string, string, double, Task>> _sentRequestEvent = new AsyncEvent<Func<string, string, double, Task>>();

        protected readonly JsonSerializer _serializer;
        protected readonly SemaphoreSlim _stateLock;
        private readonly RestClientProvider _restClientProvider;

        protected bool _isDisposed;
        private CancellationTokenSource _loginCancelToken;

        public RetryMode DefaultRetryMode { get; }
        public string UserAgent { get; }
        internal RequestQueue RequestQueue { get; }

        public LoginState LoginState { get; private set; }
        public TokenType AuthTokenType { get; private set; }
        internal string AuthToken { get; private set; }
        internal IRestClient RestClient { get; private set; }
        internal ulong? CurrentUserId { get; set; }
        public RateLimitPrecision RateLimitPrecision { get; private set; }
        internal bool UseSystemClock { get; set; }
        internal JsonSerializer Serializer => _serializer;

        /// <exception cref="ArgumentException">Unknown OAuth token type.</exception>
        public DiscordRestApiClient(RestClientProvider restClientProvider, string userAgent, RetryMode defaultRetryMode = RetryMode.AlwaysRetry,
            JsonSerializer serializer = null, RateLimitPrecision rateLimitPrecision = RateLimitPrecision.Second, bool useSystemClock = true)
        {
            _restClientProvider = restClientProvider;
            UserAgent = userAgent;
            DefaultRetryMode = defaultRetryMode;
            _serializer = serializer ?? new JsonSerializer { ContractResolver = new DiscordContractResolver() };
            RateLimitPrecision = rateLimitPrecision;
            UseSystemClock = useSystemClock;

            RequestQueue = new RequestQueue();
            _stateLock = new SemaphoreSlim(1, 1);

            SetBaseUrl(DiscordConfig.APIUrl);
        }

        /// <exception cref="ArgumentException">Unknown OAuth token type.</exception>
        internal void SetBaseUrl(string baseUrl)
        {
            RestClient?.Dispose();
            RestClient = _restClientProvider(baseUrl);
            RestClient.SetHeader("accept", "*/*");
            RestClient.SetHeader("user-agent", UserAgent);
            RestClient.SetHeader("authorization", GetPrefixedToken(AuthTokenType, AuthToken));
            RestClient.SetHeader("X-RateLimit-Precision", RateLimitPrecision.ToString().ToLower());
        }
        /// <exception cref="ArgumentException">Unknown OAuth token type.</exception>
        internal static string GetPrefixedToken(TokenType tokenType, string token)
        {
            return tokenType switch
            {
                default(TokenType) => token,
                TokenType.Bot => $"Bot {token}",
                TokenType.Bearer => $"Bearer {token}",
                _ => throw new ArgumentException(message: "Unknown OAuth token type.", paramName: nameof(tokenType)),
            };
        }
        internal virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _loginCancelToken?.Dispose();
                    RestClient?.Dispose();
                    RequestQueue?.Dispose();
                    _stateLock?.Dispose();
                }
                _isDisposed = true;
            }
        }
        public void Dispose() => Dispose(true);

        public async Task LoginAsync(TokenType tokenType, string token, RequestOptions options = null)
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LoginInternalAsync(tokenType, token, options).ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        private async Task LoginInternalAsync(TokenType tokenType, string token, RequestOptions options = null)
        {
            if (LoginState != LoginState.LoggedOut)
                await LogoutInternalAsync().ConfigureAwait(false);
            LoginState = LoginState.LoggingIn;

            try
            {
                _loginCancelToken?.Dispose();
                _loginCancelToken = new CancellationTokenSource();

                AuthToken = null;
                await RequestQueue.SetCancelTokenAsync(_loginCancelToken.Token).ConfigureAwait(false);
                RestClient.SetCancelToken(_loginCancelToken.Token);

                AuthTokenType = tokenType;
                AuthToken = token?.TrimEnd();
                if (tokenType != TokenType.Webhook)
                    RestClient.SetHeader("authorization", GetPrefixedToken(AuthTokenType, AuthToken));

                LoginState = LoginState.LoggedIn;
            }
            catch
            {
                await LogoutInternalAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task LogoutAsync()
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LogoutInternalAsync().ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        private async Task LogoutInternalAsync()
        {
            //An exception here will lock the client into the unusable LoggingOut state, but that's probably fine since our client is in an undefined state too.
            if (LoginState == LoginState.LoggedOut) return;
            LoginState = LoginState.LoggingOut;

            try { _loginCancelToken?.Cancel(false); }
            catch { }

            await DisconnectInternalAsync(null).ConfigureAwait(false);
            await RequestQueue.ClearAsync().ConfigureAwait(false);

            await RequestQueue.SetCancelTokenAsync(CancellationToken.None).ConfigureAwait(false);
            RestClient.SetCancelToken(CancellationToken.None);

            CurrentUserId = null;
            LoginState = LoginState.LoggedOut;
        }

        internal virtual Task ConnectInternalAsync() => Task.Delay(0);
        internal virtual Task DisconnectInternalAsync(Exception ex = null) => Task.Delay(0);

        //Core
        internal Task SendAsync(string method, Expression<Func<string>> endpointExpr, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null)
            => SendAsync(method, GetEndpoint(endpointExpr), GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task SendAsync(string method, string endpoint,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null)
        {
            options = options ?? new RequestOptions();
            options.HeaderOnly = true;
            options.BucketId = bucketId;

            var request = new RestRequest(RestClient, method, endpoint, options);
            await SendInternalAsync(method, endpoint, request).ConfigureAwait(false);
        }

        internal Task SendJsonAsync(string method, Expression<Func<string>> endpointExpr, object payload, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null)
            => SendJsonAsync(method, GetEndpoint(endpointExpr), payload, GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task SendJsonAsync(string method, string endpoint, object payload,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null)
        {
            options = options ?? new RequestOptions();
            options.HeaderOnly = true;
            options.BucketId = bucketId;

            string json = payload != null ? SerializeJson(payload) : null;
            var request = new JsonRestRequest(RestClient, method, endpoint, json, options);
            await SendInternalAsync(method, endpoint, request).ConfigureAwait(false);
        }

        internal Task SendMultipartAsync(string method, Expression<Func<string>> endpointExpr, IReadOnlyDictionary<string, object> multipartArgs, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null)
            => SendMultipartAsync(method, GetEndpoint(endpointExpr), multipartArgs, GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task SendMultipartAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartArgs,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null)
        {
            options = options ?? new RequestOptions();
            options.HeaderOnly = true;
            options.BucketId = bucketId;

            var request = new MultipartRestRequest(RestClient, method, endpoint, multipartArgs, options);
            await SendInternalAsync(method, endpoint, request).ConfigureAwait(false);
        }

        internal Task<TResponse> SendAsync<TResponse>(string method, Expression<Func<string>> endpointExpr, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null) where TResponse : class
            => SendAsync<TResponse>(method, GetEndpoint(endpointExpr), GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task<TResponse> SendAsync<TResponse>(string method, string endpoint,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null) where TResponse : class
        {
            options = options ?? new RequestOptions();
            options.BucketId = bucketId;

            var request = new RestRequest(RestClient, method, endpoint, options);
            return DeserializeJson<TResponse>(await SendInternalAsync(method, endpoint, request).ConfigureAwait(false));
        }

        internal Task<TResponse> SendJsonAsync<TResponse>(string method, Expression<Func<string>> endpointExpr, object payload, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null) where TResponse : class
            => SendJsonAsync<TResponse>(method, GetEndpoint(endpointExpr), payload, GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task<TResponse> SendJsonAsync<TResponse>(string method, string endpoint, object payload,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null) where TResponse : class
        {
            options = options ?? new RequestOptions();
            options.BucketId = bucketId;

            string json = payload != null ? SerializeJson(payload) : null;
            var request = new JsonRestRequest(RestClient, method, endpoint, json, options);
            return DeserializeJson<TResponse>(await SendInternalAsync(method, endpoint, request).ConfigureAwait(false));
        }

        internal Task<TResponse> SendMultipartAsync<TResponse>(string method, Expression<Func<string>> endpointExpr, IReadOnlyDictionary<string, object> multipartArgs, BucketIds ids,
             ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null, [CallerMemberName] string funcName = null)
            => SendMultipartAsync<TResponse>(method, GetEndpoint(endpointExpr), multipartArgs, GetBucketId(method, ids, endpointExpr, funcName), clientBucket, options);
        public async Task<TResponse> SendMultipartAsync<TResponse>(string method, string endpoint, IReadOnlyDictionary<string, object> multipartArgs,
            BucketId bucketId = null, ClientBucketType clientBucket = ClientBucketType.Unbucketed, RequestOptions options = null)
        {
            options = options ?? new RequestOptions();
            options.BucketId = bucketId;

            var request = new MultipartRestRequest(RestClient, method, endpoint, multipartArgs, options);
            return DeserializeJson<TResponse>(await SendInternalAsync(method, endpoint, request).ConfigureAwait(false));
        }

        private async Task<Stream> SendInternalAsync(string method, string endpoint, RestRequest request)
        {
            if (!request.Options.IgnoreState)
                CheckState();
            if (request.Options.RetryMode == null)
                request.Options.RetryMode = DefaultRetryMode;
            if (request.Options.UseSystemClock == null)
                request.Options.UseSystemClock = UseSystemClock;

            var stopwatch = Stopwatch.StartNew();
            var responseStream = await RequestQueue.SendAsync(request).ConfigureAwait(false);
            stopwatch.Stop();

            double milliseconds = ToMilliseconds(stopwatch);
            await _sentRequestEvent.InvokeAsync(method, endpoint, milliseconds).ConfigureAwait(false);

            return responseStream;
        }

        //Auth
        public async Task ValidateTokenAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            await SendAsync("GET", () => "auth/login", new BucketIds(), options: options).ConfigureAwait(false);
        }

        //Gateway
        public async Task<GetGatewayResponse> GetGatewayAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<GetGatewayResponse>("GET", () => "gateway", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<GetBotGatewayResponse> GetBotGatewayAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<GetBotGatewayResponse>("GET", () => "gateway/bot", new BucketIds(), options: options).ConfigureAwait(false);
        }

        //Channels
        public async Task<Channel> GetChannelAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(channelId: channelId);
                return await SendAsync<Channel>("GET", () => $"channels/{channelId}", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<Channel> GetChannelAsync(ulong guildId, ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(channelId: channelId);
                var model = await SendAsync<Channel>("GET", () => $"channels/{channelId}", ids, options: options).ConfigureAwait(false);
                if (!model.GuildId.IsSpecified || model.GuildId.Value != guildId)
                    return null;
                return model;
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<IReadOnlyCollection<Channel>> GetGuildChannelsAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<Channel>>("GET", () => $"guilds/{guildId}/channels", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Channel> CreateGuildChannelAsync(ulong guildId, CreateGuildChannelParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.Bitrate, 0, nameof(args.Bitrate));
            Preconditions.NotNullOrWhitespace(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Channel>("POST", () => $"guilds/{guildId}/channels", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Channel> DeleteChannelAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendAsync<Channel>("DELETE", () => $"channels/{channelId}", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException">
        /// <paramref name="channelId"/> must not be equal to zero.
        /// -and-
        /// <paramref name="args.Position"/> must be greater than zero.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="args"/> must not be <see langword="null"/>.
        /// -and-
        /// <paramref name="args.Name"/> must not be <see langword="null"/> or empty.
        /// </exception>
        public async Task<Channel> ModifyGuildChannelAsync(ulong channelId, Rest.ModifyGuildChannelParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Position, 0, nameof(args.Position));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<Channel>("PATCH", () => $"channels/{channelId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Channel> ModifyGuildChannelAsync(ulong channelId, Rest.ModifyTextChannelParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Position, 0, nameof(args.Position));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            Preconditions.AtLeast(args.SlowModeInterval, 0, nameof(args.SlowModeInterval));
            Preconditions.AtMost(args.SlowModeInterval, 21600, nameof(args.SlowModeInterval));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<Channel>("PATCH", () => $"channels/{channelId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Channel> ModifyGuildChannelAsync(ulong channelId, Rest.ModifyVoiceChannelParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Bitrate, 8000, nameof(args.Bitrate));
            Preconditions.AtLeast(args.UserLimit, 0, nameof(args.UserLimit));
            Preconditions.AtLeast(args.Position, 0, nameof(args.Position));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<Channel>("PATCH", () => $"channels/{channelId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task ModifyGuildChannelsAsync(ulong guildId, IEnumerable<Rest.ModifyGuildChannelsParams> args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            var channels = args.ToArray();
            switch (channels.Length)
            {
                case 0:
                    return;
                case 1:
                    await ModifyGuildChannelAsync(channels[0].Id, new Rest.ModifyGuildChannelParams { Position = channels[0].Position }).ConfigureAwait(false);
                    break;
                default:
                    var ids = new BucketIds(guildId: guildId);
                    await SendJsonAsync("PATCH", () => $"guilds/{guildId}/channels", channels, ids, options: options).ConfigureAwait(false);
                    break;
            }
        }
        public async Task AddRoleAsync(ulong guildId, ulong userId, ulong roleId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotEqual(roleId, 0, nameof(roleId));
            Preconditions.NotEqual(roleId, guildId, nameof(roleId), "The Everyone role cannot be added to a user.");
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendAsync("PUT", () => $"guilds/{guildId}/members/{userId}/roles/{roleId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task RemoveRoleAsync(ulong guildId, ulong userId, ulong roleId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotEqual(roleId, 0, nameof(roleId));
            Preconditions.NotEqual(roleId, guildId, nameof(roleId), "The Everyone role cannot be removed from a user.");
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendAsync("DELETE", () => $"guilds/{guildId}/members/{userId}/roles/{roleId}", ids, options: options).ConfigureAwait(false);
        }

        //Channel Messages
        public async Task<Message> GetChannelMessageAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(channelId: channelId);
                return await SendAsync<Message>("GET", () => $"channels/{channelId}/messages/{messageId}", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<IReadOnlyCollection<Message>> GetChannelMessagesAsync(ulong channelId, GetChannelMessagesParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Limit, 0, nameof(args.Limit));
            Preconditions.AtMost(args.Limit, DiscordConfig.MaxMessagesPerBatch, nameof(args.Limit));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(DiscordConfig.MaxMessagesPerBatch);
            ulong? relativeId = args.RelativeMessageId.IsSpecified ? args.RelativeMessageId.Value : (ulong?)null;
            string relativeDir;

            switch (args.RelativeDirection.GetValueOrDefault(Direction.Before))
            {
                case Direction.Before:
                default:
                    relativeDir = "before";
                    break;
                case Direction.After:
                    relativeDir = "after";
                    break;
                case Direction.Around:
                    relativeDir = "around";
                    break;
            }

            var ids = new BucketIds(channelId: channelId);
            Expression<Func<string>> endpoint;
            if (relativeId != null)
                endpoint = () => $"channels/{channelId}/messages?limit={limit}&{relativeDir}={relativeId}";
            else
                endpoint = () => $"channels/{channelId}/messages?limit={limit}";
            return await SendAsync<IReadOnlyCollection<Message>>("GET", endpoint, ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public async Task<Message> CreateMessageAsync(ulong channelId, CreateMessageParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            if (!args.Embed.IsSpecified || args.Embed.Value == null)
                Preconditions.NotNullOrEmpty(args.Content, nameof(args.Content));

            if (args.Content?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentException(message: $"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", paramName: nameof(args.Content));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<Message>("POST", () => $"channels/{channelId}/messages", args, ids, clientBucket: ClientBucketType.SendEdit, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        /// <exception cref="InvalidOperationException">This operation may only be called with a <see cref="TokenType.Webhook"/> token.</exception>
        public async Task<Message> CreateWebhookMessageAsync(ulong webhookId, CreateWebhookMessageParams args, RequestOptions options = null)
        {
            if (AuthTokenType != TokenType.Webhook)
                throw new InvalidOperationException($"This operation may only be called with a {nameof(TokenType.Webhook)} token.");

            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(webhookId, 0, nameof(webhookId));
            if (!args.Embeds.IsSpecified || args.Embeds.Value == null || args.Embeds.Value.Length == 0)
                Preconditions.NotNullOrEmpty(args.Content, nameof(args.Content));

            if (args.Content?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentException(message: $"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", paramName: nameof(args.Content));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(webhookId: webhookId);
            return await SendJsonAsync<Message>("POST", () => $"webhooks/{webhookId}/{AuthToken}?wait=true", args, ids, clientBucket: ClientBucketType.SendEdit, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public async Task<Message> UploadFileAsync(ulong channelId, UploadFileParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            if (args.Content.GetValueOrDefault(null) == null)
                args.Content = "";
            else if (args.Content.IsSpecified && args.Content.Value?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentOutOfRangeException($"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", nameof(args.Content));

            var ids = new BucketIds(channelId: channelId);
            return await SendMultipartAsync<Message>("POST", () => $"channels/{channelId}/messages", args.ToDictionary(), ids, clientBucket: ClientBucketType.SendEdit, options: options).ConfigureAwait(false);
        }

        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        /// <exception cref="InvalidOperationException">This operation may only be called with a <see cref="TokenType.Webhook"/> token.</exception>
        public async Task<Message> UploadWebhookFileAsync(ulong webhookId, UploadWebhookFileParams args, RequestOptions options = null)
        {
            if (AuthTokenType != TokenType.Webhook)
                throw new InvalidOperationException($"This operation may only be called with a {nameof(TokenType.Webhook)} token.");

            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(webhookId, 0, nameof(webhookId));
            options = RequestOptions.CreateOrClone(options);

            if (args.Content.GetValueOrDefault(null) == null)
                args.Content = "";
            else if (args.Content.IsSpecified)
            {
                if (args.Content.Value == null)
                    args.Content = "";
                if (args.Content.Value?.Length > DiscordConfig.MaxMessageSize)
                    throw new ArgumentOutOfRangeException($"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", nameof(args.Content));
            }

            var ids = new BucketIds(webhookId: webhookId);
            return await SendMultipartAsync<Message>("POST", () => $"webhooks/{webhookId}/{AuthToken}?wait=true", args.ToDictionary(), ids, clientBucket: ClientBucketType.SendEdit, options: options).ConfigureAwait(false);
        }
        public async Task DeleteMessageAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("DELETE", () => $"channels/{channelId}/messages/{messageId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task DeleteMessagesAsync(ulong channelId, DeleteMessagesParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNull(args.MessageIds, nameof(args.MessageIds));
            Preconditions.AtMost(args.MessageIds.Length, 100, nameof(args.MessageIds.Length));
            Preconditions.YoungerThanTwoWeeks(args.MessageIds, nameof(args.MessageIds));
            options = RequestOptions.CreateOrClone(options);

            switch (args.MessageIds.Length)
            {
                case 0:
                    return;
                case 1:
                    await DeleteMessageAsync(channelId, args.MessageIds[0]).ConfigureAwait(false);
                    break;
                default:
                    var ids = new BucketIds(channelId: channelId);
                    await SendJsonAsync("POST", () => $"channels/{channelId}/messages/bulk-delete", args, ids, options: options).ConfigureAwait(false);
                    break;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Message content is too long, length must be less or equal to <see cref="DiscordConfig.MaxMessageSize"/>.</exception>
        public async Task<Message> ModifyMessageAsync(ulong channelId, ulong messageId, Rest.ModifyMessageParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            Preconditions.NotNull(args, nameof(args));
            if (args.Content.IsSpecified && args.Content.Value?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentOutOfRangeException($"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", nameof(args.Content));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<Message>("PATCH", () => $"channels/{channelId}/messages/{messageId}", args, ids, clientBucket: ClientBucketType.SendEdit, options: options).ConfigureAwait(false);
        }

        public async Task SuppressEmbedAsync(ulong channelId, ulong messageId, Rest.SuppressEmbedParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendJsonAsync("POST", () => $"channels/{channelId}/messages/{messageId}/suppress-embeds", args, ids, options: options).ConfigureAwait(false);
        }

        public async Task AddReactionAsync(ulong channelId, ulong messageId, string emoji, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            Preconditions.NotNullOrWhitespace(emoji, nameof(emoji));

            options = RequestOptions.CreateOrClone(options);
            options.IsReactionBucket = true;

            var ids = new BucketIds(channelId: channelId);

            // @me is non-const to fool the ratelimiter, otherwise it will put add/remove in separate buckets
            var me = "@me";
            await SendAsync("PUT", () => $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/{me}", ids, options: options).ConfigureAwait(false);
        }
        public async Task RemoveReactionAsync(ulong channelId, ulong messageId, ulong userId, string emoji, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            Preconditions.NotNullOrWhitespace(emoji, nameof(emoji));

            options = RequestOptions.CreateOrClone(options);
            options.IsReactionBucket = true;

            var ids = new BucketIds(channelId: channelId);

            var user = CurrentUserId.HasValue ? (userId == CurrentUserId.Value ? "@me" : userId.ToString()) : userId.ToString();
            await SendAsync("DELETE", () => $"channels/{channelId}/messages/{messageId}/reactions/{emoji}/{user}", ids, options: options).ConfigureAwait(false);
        }
        public async Task RemoveAllReactionsAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));

            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);

            await SendAsync("DELETE", () => $"channels/{channelId}/messages/{messageId}/reactions", ids, options: options).ConfigureAwait(false);
        }
        public async Task RemoveAllReactionsForEmoteAsync(ulong channelId, ulong messageId, string emoji, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            Preconditions.NotNullOrWhitespace(emoji, nameof(emoji));

            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);

            await SendAsync("DELETE", () => $"channels/{channelId}/messages/{messageId}/reactions/{emoji}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<User>> GetReactionUsersAsync(ulong channelId, ulong messageId, string emoji, GetReactionUsersParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            Preconditions.NotNullOrWhitespace(emoji, nameof(emoji));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.Limit, 0, nameof(args.Limit));
            Preconditions.AtMost(args.Limit, DiscordConfig.MaxUserReactionsPerBatch, nameof(args.Limit));
            Preconditions.GreaterThan(args.AfterUserId, 0, nameof(args.AfterUserId));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(DiscordConfig.MaxUserReactionsPerBatch);
            ulong afterUserId = args.AfterUserId.GetValueOrDefault(0);

            var ids = new BucketIds(channelId: channelId);
            Expression<Func<string>> endpoint = () => $"channels/{channelId}/messages/{messageId}/reactions/{emoji}?limit={limit}&after={afterUserId}";
            return await SendAsync<IReadOnlyCollection<User>>("GET", endpoint, ids, options: options).ConfigureAwait(false);
        }
        public async Task AckMessageAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("POST", () => $"channels/{channelId}/messages/{messageId}/ack", ids, options: options).ConfigureAwait(false);
        }
        public async Task TriggerTypingIndicatorAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("POST", () => $"channels/{channelId}/typing", ids, options: options).ConfigureAwait(false);
        }
        public async Task CrosspostAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("POST", () => $"channels/{channelId}/messages/{messageId}/crosspost", ids, options: options).ConfigureAwait(false);
        }

        //Channel Permissions
        public async Task ModifyChannelPermissionsAsync(ulong channelId, ulong targetId, ModifyChannelPermissionsParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(targetId, 0, nameof(targetId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendJsonAsync("PUT", () => $"channels/{channelId}/permissions/{targetId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task DeleteChannelPermissionAsync(ulong channelId, ulong targetId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(targetId, 0, nameof(targetId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("DELETE", () => $"channels/{channelId}/permissions/{targetId}", ids, options: options).ConfigureAwait(false);
        }

        //Channel Pins
        public async Task AddPinAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.GreaterThan(channelId, 0, nameof(channelId));
            Preconditions.GreaterThan(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("PUT", () => $"channels/{channelId}/pins/{messageId}", ids, options: options).ConfigureAwait(false);

        }
        public async Task RemovePinAsync(ulong channelId, ulong messageId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(messageId, 0, nameof(messageId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("DELETE", () => $"channels/{channelId}/pins/{messageId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Message>> GetPinsAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendAsync<IReadOnlyCollection<Message>>("GET", () => $"channels/{channelId}/pins", ids, options: options).ConfigureAwait(false);
        }

        //Channel Recipients
        public async Task AddGroupRecipientAsync(ulong channelId, ulong userId, RequestOptions options = null)
        {
            Preconditions.GreaterThan(channelId, 0, nameof(channelId));
            Preconditions.GreaterThan(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("PUT", () => $"channels/{channelId}/recipients/{userId}", ids, options: options).ConfigureAwait(false);

        }
        public async Task RemoveGroupRecipientAsync(ulong channelId, ulong userId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            await SendAsync("DELETE", () => $"channels/{channelId}/recipients/{userId}", ids, options: options).ConfigureAwait(false);
        }

        //Interactions
        public async Task<ApplicationCommand[]> GetGlobalApplicationCommandsAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            return await SendAsync<ApplicationCommand[]>("GET", () => $"applications/{this.CurrentUserId}/commands", new BucketIds(), options: options).ConfigureAwait(false);
        }


        public async Task<ApplicationCommand> CreateGlobalApplicationCommandAsync(CreateApplicationCommandParams command, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));
            Preconditions.AtMost(command.Name.Length, 32, nameof(command.Name));
            Preconditions.AtLeast(command.Name.Length, 3, nameof(command.Name));
            Preconditions.AtMost(command.Description.Length, 100, nameof(command.Description));
            Preconditions.AtLeast(command.Description.Length, 1, nameof(command.Description));

            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<ApplicationCommand>("POST", () => $"applications/{this.CurrentUserId}/commands", command, new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<ApplicationCommand> ModifyGlobalApplicationCommandAsync(ModifyApplicationCommandParams command, ulong commandId, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));

            if (command.Name.IsSpecified)
            {
                Preconditions.AtMost(command.Name.Value.Length, 32, nameof(command.Name));
                Preconditions.AtLeast(command.Name.Value.Length, 3, nameof(command.Name));
            }
            if (command.Description.IsSpecified)
            {
                Preconditions.AtMost(command.Description.Value.Length, 100, nameof(command.Description));
                Preconditions.AtLeast(command.Description.Value.Length, 1, nameof(command.Description));
            }

            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<ApplicationCommand>("PATCH", () => $"applications/{this.CurrentUserId}/commands/{commandId}", command, new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task DeleteGlobalApplicationCommandAsync(ulong commandId, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            await SendAsync("DELETE", () => $"applications/{this.CurrentUserId}/commands/{commandId}", new BucketIds(), options: options).ConfigureAwait(false);
        }

        public async Task<ApplicationCommand[]> GetGuildApplicationCommandAsync(ulong guildId, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            var bucket = new BucketIds(guildId: guildId);

            return await SendAsync<ApplicationCommand[]>("GET", () => $"applications/{this.CurrentUserId}/guilds/{guildId}/commands", bucket, options: options).ConfigureAwait(false);
        }
        public async Task<ApplicationCommand> CreateGuildApplicationCommandAsync(CreateApplicationCommandParams command, ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));
            Preconditions.AtMost(command.Name.Length, 32, nameof(command.Name));
            Preconditions.AtLeast(command.Name.Length, 3, nameof(command.Name));
            Preconditions.AtMost(command.Description.Length, 100, nameof(command.Description));
            Preconditions.AtLeast(command.Description.Length, 1, nameof(command.Description));

            options = RequestOptions.CreateOrClone(options);

            var bucket = new BucketIds(guildId: guildId);

            return await SendJsonAsync<ApplicationCommand>("POST", () => $"applications/{this.CurrentUserId}/guilds/{guildId}/commands", command, bucket, options: options).ConfigureAwait(false);
        }
        public async Task<ApplicationCommand> ModifyGuildApplicationCommandAsync(ModifyApplicationCommandParams command, ulong guildId, ulong commandId, RequestOptions options = null)
        {
            Preconditions.NotNull(command, nameof(command));

            if (command.Name.IsSpecified)
            {
                Preconditions.AtMost(command.Name.Value.Length, 32, nameof(command.Name));
                Preconditions.AtLeast(command.Name.Value.Length, 3, nameof(command.Name));
            }
            if (command.Description.IsSpecified)
            {
                Preconditions.AtMost(command.Description.Value.Length, 100, nameof(command.Description));
                Preconditions.AtLeast(command.Description.Value.Length, 1, nameof(command.Description));
            }

            options = RequestOptions.CreateOrClone(options);

            var bucket = new BucketIds(guildId: guildId);

            return await SendJsonAsync<ApplicationCommand>("PATCH", () => $"applications/{this.CurrentUserId}/guilds/{guildId}/commands/{commandId}", command, bucket, options: options).ConfigureAwait(false);
        }
        public async Task DeleteGuildApplicationCommandAsync(ulong guildId, ulong commandId, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            var bucket = new BucketIds(guildId: guildId);

            await SendAsync<ApplicationCommand>("DELETE", () => $"applications/{this.CurrentUserId}/guilds/{guildId}/commands/{commandId}", bucket, options: options).ConfigureAwait(false);
        }

        //Interaction Responses
        public async Task CreateInteractionResponse(InteractionResponse response, ulong interactionId, string interactionToken, RequestOptions options = null)
        {
            if(response.Data.IsSpecified && response.Data.Value.Content.IsSpecified)
                Preconditions.AtMost(response.Data.Value.Content.Value.Length, 2000, nameof(response.Data.Value.Content));

            options = RequestOptions.CreateOrClone(options);

            await SendJsonAsync("POST", () => $"interactions/{interactionId}/{interactionToken}/callback", response, new BucketIds(), options: options);
        }
        public async Task ModifyInteractionResponse(ModifyInteractionResponseParams args, string interactionToken, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            await SendJsonAsync("POST", () => $"webhooks/{this.CurrentUserId}/{interactionToken}/messages/@original", args, new BucketIds(), options: options);
        }
        public async Task DeleteInteractionResponse(string interactionToken, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);

            await SendAsync("DELETE", () => $"webhooks/{this.CurrentUserId}/{interactionToken}/messages/@original", new BucketIds(), options: options);
        }

        public async Task<Message> CreateInteractionFollowupMessage(CreateWebhookMessageParams args, string token, RequestOptions options = null)
        {
            if (!args.Embeds.IsSpecified || args.Embeds.Value == null || args.Embeds.Value.Length == 0)
                Preconditions.NotNullOrEmpty(args.Content, nameof(args.Content));

            if (args.Content?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentException(message: $"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", paramName: nameof(args.Content));

            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<Message>("POST", () => $"webhooks/{CurrentUserId}/{token}?wait=true", args, new BucketIds(), options: options).ConfigureAwait(false);
        }

        public async Task<Message> ModifyInteractionFollowupMessage(CreateWebhookMessageParams args, ulong id, string token, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(id, 0, nameof(id));

            if (args.Content?.Length > DiscordConfig.MaxMessageSize)
                throw new ArgumentException(message: $"Message content is too long, length must be less or equal to {DiscordConfig.MaxMessageSize}.", paramName: nameof(args.Content));

            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<Message>("PATCH", () => $"webhooks/{CurrentUserId}/{token}/messages/{id}", args, new BucketIds(), options: options).ConfigureAwait(false);
        }

        public async Task DeleteInteractionFollowupMessage(ulong id, string token, RequestOptions options = null)
        {
            Preconditions.NotEqual(id, 0, nameof(id));

            options = RequestOptions.CreateOrClone(options);

            await SendAsync("DELETE", () => $"webhooks/{CurrentUserId}/{token}/messages/{id}", new BucketIds(), options: options).ConfigureAwait(false);
        }

        //Guilds
        public async Task<Guild> GetGuildAsync(ulong guildId, bool withCounts, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(guildId: guildId);
                return await SendAsync<Guild>("GET", () => $"guilds/{guildId}?with_counts={(withCounts ? "true" : "false")}", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<Guild> CreateGuildAsync(CreateGuildParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNullOrWhitespace(args.Name, nameof(args.Name));
            Preconditions.NotNullOrWhitespace(args.RegionId, nameof(args.RegionId));
            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<Guild>("POST", () => "guilds", args, new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<Guild> DeleteGuildAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Guild>("DELETE", () => $"guilds/{guildId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Guild> LeaveGuildAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Guild>("DELETE", () => $"users/@me/guilds/{guildId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Guild> ModifyGuildAsync(ulong guildId, Rest.ModifyGuildParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(args.AfkChannelId, 0, nameof(args.AfkChannelId));
            Preconditions.AtLeast(args.AfkTimeout, 0, nameof(args.AfkTimeout));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            Preconditions.GreaterThan(args.OwnerId, 0, nameof(args.OwnerId));
            Preconditions.NotNull(args.RegionId, nameof(args.RegionId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Guild>("PATCH", () => $"guilds/{guildId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<GetGuildPruneCountResponse> BeginGuildPruneAsync(ulong guildId, GuildPruneParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Days, 1, nameof(args.Days));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<GetGuildPruneCountResponse>("POST", () => $"guilds/{guildId}/prune", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<GetGuildPruneCountResponse> GetGuildPruneCountAsync(ulong guildId, GuildPruneParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Days, 1, nameof(args.Days));
            string endpointRoleIds = args.IncludeRoleIds?.Length > 0 ? $"&include_roles={string.Join(",", args.IncludeRoleIds)}" : "";
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<GetGuildPruneCountResponse>("GET", () => $"guilds/{guildId}/prune?days={args.Days}{endpointRoleIds}", ids, options: options).ConfigureAwait(false);
        }

        //Guild Bans
        public async Task<IReadOnlyCollection<Ban>> GetGuildBansAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<Ban>>("GET", () => $"guilds/{guildId}/bans", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Ban> GetGuildBanAsync(ulong guildId, ulong userId, RequestOptions options)
        {
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(guildId: guildId);
                return await SendAsync<Ban>("GET", () => $"guilds/{guildId}/bans/{userId}", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        /// <exception cref="ArgumentException">
        /// <paramref name="guildId"/> and <paramref name="userId"/> must not be equal to zero.
        /// -and-
        /// <paramref name="args.DeleteMessageDays"/> must be between 0 to 7.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> must not be <see langword="null"/>.</exception>
        public async Task CreateGuildBanAsync(ulong guildId, ulong userId, CreateGuildBanParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.DeleteMessageDays, 0, nameof(args.DeleteMessageDays), "Prune length must be within [0, 7]");
            Preconditions.AtMost(args.DeleteMessageDays, 7, nameof(args.DeleteMessageDays), "Prune length must be within [0, 7]");
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            string reason = string.IsNullOrWhiteSpace(args.Reason) ? "" : $"&reason={Uri.EscapeDataString(args.Reason)}";
            await SendAsync("PUT", () => $"guilds/{guildId}/bans/{userId}?delete-message-days={args.DeleteMessageDays}{reason}", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> and <paramref name="userId"/> must not be equal to zero.</exception>
        public async Task RemoveGuildBanAsync(ulong guildId, ulong userId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendAsync("DELETE", () => $"guilds/{guildId}/bans/{userId}", ids, options: options).ConfigureAwait(false);
        }

        //Guild Embeds
        /// <exception cref="ArgumentException"><paramref name="guildId"/> must not be equal to zero.</exception>
        public async Task<GuildEmbed> GetGuildEmbedAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(guildId: guildId);
                return await SendAsync<GuildEmbed>("GET", () => $"guilds/{guildId}/embed", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> must not be equal to zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> must not be <see langword="null"/>.</exception>
        public async Task<GuildEmbed> ModifyGuildEmbedAsync(ulong guildId, Rest.ModifyGuildEmbedParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<GuildEmbed>("PATCH", () => $"guilds/{guildId}/embed", args, ids, options: options).ConfigureAwait(false);
        }

        //Guild Widget
        /// <exception cref="ArgumentException"><paramref name="guildId"/> must not be equal to zero.</exception>
        public async Task<GuildWidget> GetGuildWidgetAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(guildId: guildId);
                return await SendAsync<GuildWidget>("GET", () => $"guilds/{guildId}/widget", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> must not be equal to zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> must not be <see langword="null"/>.</exception>
        public async Task<GuildWidget> ModifyGuildWidgetAsync(ulong guildId, Rest.ModifyGuildWidgetParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<GuildWidget>("PATCH", () => $"guilds/{guildId}/widget", args, ids, options: options).ConfigureAwait(false);
        }

        //Guild Integrations
        /// <exception cref="ArgumentException"><paramref name="guildId"/> must not be equal to zero.</exception>
        public async Task<IReadOnlyCollection<Integration>> GetGuildIntegrationsAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<Integration>>("GET", () => $"guilds/{guildId}/integrations", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> and <paramref name="args.Id"/> must not be equal to zero.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> must not be <see langword="null"/>.</exception>
        public async Task<Integration> CreateGuildIntegrationAsync(ulong guildId, CreateGuildIntegrationParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotEqual(args.Id, 0, nameof(args.Id));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Integration>("POST", () => $"guilds/{guildId}/integrations", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Integration> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(integrationId, 0, nameof(integrationId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Integration>("DELETE", () => $"guilds/{guildId}/integrations/{integrationId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Integration> ModifyGuildIntegrationAsync(ulong guildId, ulong integrationId, Rest.ModifyGuildIntegrationParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(integrationId, 0, nameof(integrationId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.ExpireBehavior, 0, nameof(args.ExpireBehavior));
            Preconditions.AtLeast(args.ExpireGracePeriod, 0, nameof(args.ExpireGracePeriod));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Integration>("PATCH", () => $"guilds/{guildId}/integrations/{integrationId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Integration> SyncGuildIntegrationAsync(ulong guildId, ulong integrationId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(integrationId, 0, nameof(integrationId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Integration>("POST", () => $"guilds/{guildId}/integrations/{integrationId}/sync", ids, options: options).ConfigureAwait(false);
        }

        //Guild Invites
        /// <exception cref="ArgumentException"><paramref name="inviteId"/> cannot be blank.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inviteId"/> must not be <see langword="null"/>.</exception>
        public async Task<InviteMetadata> GetInviteAsync(string inviteId, RequestOptions options = null)
        {
            Preconditions.NotNullOrEmpty(inviteId, nameof(inviteId));
            options = RequestOptions.CreateOrClone(options);

            //Remove trailing slash
            if (inviteId[inviteId.Length - 1] == '/')
                inviteId = inviteId.Substring(0, inviteId.Length - 1);
            //Remove leading URL
            int index = inviteId.LastIndexOf('/');
            if (index >= 0)
                inviteId = inviteId.Substring(index + 1);

            try
            {
                return await SendAsync<InviteMetadata>("GET", () => $"invites/{inviteId}?with_counts=true", new BucketIds(), options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> may not be equal to zero.</exception>
        public async Task<InviteVanity> GetVanityInviteAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<InviteVanity>("GET", () => $"guilds/{guildId}/vanity-url", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException"><paramref name="guildId"/> may not be equal to zero.</exception>
        public async Task<IReadOnlyCollection<InviteMetadata>> GetGuildInvitesAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<InviteMetadata>>("GET", () => $"guilds/{guildId}/invites", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException"><paramref name="channelId"/> may not be equal to zero.</exception>
        public async Task<IReadOnlyCollection<InviteMetadata>> GetChannelInvitesAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendAsync<IReadOnlyCollection<InviteMetadata>>("GET", () => $"channels/{channelId}/invites", ids, options: options).ConfigureAwait(false);
        }
        /// <exception cref="ArgumentException">
        /// <paramref name="channelId"/> may not be equal to zero.
        /// -and-
        /// <paramref name="args.MaxAge"/> and <paramref name="args.MaxUses"/> must be greater than zero.
        /// -and-
        /// <paramref name="args.MaxAge"/> must be lesser than 86400.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> must not be <see langword="null"/>.</exception>
        public async Task<InviteMetadata> CreateChannelInviteAsync(ulong channelId, CreateChannelInviteParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.MaxAge, 0, nameof(args.MaxAge));
            Preconditions.AtLeast(args.MaxUses, 0, nameof(args.MaxUses));
            Preconditions.AtMost(args.MaxAge, 86400, nameof(args.MaxAge),
                "The maximum age of an invite must be less than or equal to a day (86400 seconds).");
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendJsonAsync<InviteMetadata>("POST", () => $"channels/{channelId}/invites", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Invite> DeleteInviteAsync(string inviteId, RequestOptions options = null)
        {
            Preconditions.NotNullOrEmpty(inviteId, nameof(inviteId));
            options = RequestOptions.CreateOrClone(options);

            return await SendAsync<Invite>("DELETE", () => $"invites/{inviteId}", new BucketIds(), options: options).ConfigureAwait(false);
        }

        //Guild Members
        public async Task<GuildMember> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNullOrWhitespace(args.AccessToken, nameof(args.AccessToken));

            if (args.RoleIds.IsSpecified)
            {
                foreach (var roleId in args.RoleIds.Value)
                    Preconditions.NotEqual(roleId, 0, nameof(roleId));
            }

            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);

            return await SendJsonAsync<GuildMember>("PUT", () => $"guilds/{guildId}/members/{userId}", args, ids, options: options);
        }
        public async Task<GuildMember> GetGuildMemberAsync(ulong guildId, ulong userId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                var ids = new BucketIds(guildId: guildId);
                return await SendAsync<GuildMember>("GET", () => $"guilds/{guildId}/members/{userId}", ids, options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<IReadOnlyCollection<GuildMember>> GetGuildMembersAsync(ulong guildId, GetGuildMembersParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.Limit, 0, nameof(args.Limit));
            Preconditions.AtMost(args.Limit, DiscordConfig.MaxUsersPerBatch, nameof(args.Limit));
            Preconditions.GreaterThan(args.AfterUserId, 0, nameof(args.AfterUserId));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(int.MaxValue);
            ulong afterUserId = args.AfterUserId.GetValueOrDefault(0);

            var ids = new BucketIds(guildId: guildId);
            Expression<Func<string>> endpoint = () => $"guilds/{guildId}/members?limit={limit}&after={afterUserId}";
            return await SendAsync<IReadOnlyCollection<GuildMember>>("GET", endpoint, ids, options: options).ConfigureAwait(false);
        }
        public async Task RemoveGuildMemberAsync(ulong guildId, ulong userId, string reason, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            reason = string.IsNullOrWhiteSpace(reason) ? "" : $"?reason={Uri.EscapeDataString(reason)}";
            await SendAsync("DELETE", () => $"guilds/{guildId}/members/{userId}{reason}", ids, options: options).ConfigureAwait(false);
        }
        public async Task ModifyGuildMemberAsync(ulong guildId, ulong userId, Rest.ModifyGuildMemberParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(userId, 0, nameof(userId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            bool isCurrentUser = userId == CurrentUserId;

            if (args.RoleIds.IsSpecified)
                Preconditions.NotEveryoneRole(args.RoleIds.Value, guildId, nameof(args.RoleIds));
            if (isCurrentUser && args.Nickname.IsSpecified)
            {
                var nickArgs = new Rest.ModifyCurrentUserNickParams(args.Nickname.Value ?? "");
                await ModifyMyNickAsync(guildId, nickArgs).ConfigureAwait(false);
                args.Nickname = Optional.Create<string>(); //Remove
            }
            if (!isCurrentUser || args.Deaf.IsSpecified || args.Mute.IsSpecified || args.RoleIds.IsSpecified)
            {
                var ids = new BucketIds(guildId: guildId);
                await SendJsonAsync("PATCH", () => $"guilds/{guildId}/members/{userId}", args, ids, options: options).ConfigureAwait(false);
            }
        }
        public async Task<IReadOnlyCollection<GuildMember>> SearchGuildMembersAsync(ulong guildId, SearchGuildMembersParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.Limit, 0, nameof(args.Limit));
            Preconditions.AtMost(args.Limit, DiscordConfig.MaxUsersPerBatch, nameof(args.Limit));
            Preconditions.NotNullOrEmpty(args.Query, nameof(args.Query));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(DiscordConfig.MaxUsersPerBatch);
            string query = args.Query;

            var ids = new BucketIds(guildId: guildId);
            Expression<Func<string>> endpoint = () => $"guilds/{guildId}/members/search?limit={limit}&query={query}";
            return await SendAsync<IReadOnlyCollection<GuildMember>>("GET", endpoint, ids, options: options).ConfigureAwait(false);
        }

        //Guild Roles
        public async Task<IReadOnlyCollection<Role>> GetGuildRolesAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<Role>>("GET", () => $"guilds/{guildId}/roles", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Role> CreateGuildRoleAsync(ulong guildId, Rest.ModifyGuildRoleParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Role>("POST", () => $"guilds/{guildId}/roles", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task DeleteGuildRoleAsync(ulong guildId, ulong roleId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(roleId, 0, nameof(roleId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendAsync("DELETE", () => $"guilds/{guildId}/roles/{roleId}", ids, options: options).ConfigureAwait(false);
        }
        public async Task<Role> ModifyGuildRoleAsync(ulong guildId, ulong roleId, Rest.ModifyGuildRoleParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(roleId, 0, nameof(roleId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.AtLeast(args.Color, 0, nameof(args.Color));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Role>("PATCH", () => $"guilds/{guildId}/roles/{roleId}", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Role>> ModifyGuildRolesAsync(ulong guildId, IEnumerable<Rest.ModifyGuildRolesParams> args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<IReadOnlyCollection<Role>>("PATCH", () => $"guilds/{guildId}/roles", args, ids, options: options).ConfigureAwait(false);
        }

        //Guild emoji
        public async Task<Emoji> GetGuildEmoteAsync(ulong guildId, ulong emoteId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(emoteId, 0, nameof(emoteId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<Emoji>("GET", () => $"guilds/{guildId}/emojis/{emoteId}", ids, options: options).ConfigureAwait(false);
        }

        public async Task<Emoji> CreateGuildEmoteAsync(ulong guildId, Rest.CreateGuildEmoteParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNullOrWhitespace(args.Name, nameof(args.Name));
            Preconditions.NotNull(args.Image.Stream, nameof(args.Image));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Emoji>("POST", () => $"guilds/{guildId}/emojis", args, ids, options: options).ConfigureAwait(false);
        }

        public async Task<Emoji> ModifyGuildEmoteAsync(ulong guildId, ulong emoteId, ModifyGuildEmoteParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(emoteId, 0, nameof(emoteId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendJsonAsync<Emoji>("PATCH", () => $"guilds/{guildId}/emojis/{emoteId}", args, ids, options: options).ConfigureAwait(false);
        }

        public async Task DeleteGuildEmoteAsync(ulong guildId, ulong emoteId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotEqual(emoteId, 0, nameof(emoteId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendAsync("DELETE", () => $"guilds/{guildId}/emojis/{emoteId}", ids, options: options).ConfigureAwait(false);
        }

        //Users
        public async Task<User> GetUserAsync(ulong userId, RequestOptions options = null)
        {
            Preconditions.NotEqual(userId, 0, nameof(userId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                return await SendAsync<User>("GET", () => $"users/{userId}", new BucketIds(), options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }

        //Current User/DMs
        public async Task<User> GetMyUserAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<User>("GET", () => "users/@me", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Connection>> GetMyConnectionsAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<IReadOnlyCollection<Connection>>("GET", () => "users/@me/connections", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Channel>> GetMyPrivateChannelsAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<IReadOnlyCollection<Channel>>("GET", () => "users/@me/channels", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<UserGuild>> GetMyGuildsAsync(GetGuildSummariesParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.Limit, 0, nameof(args.Limit));
            Preconditions.AtMost(args.Limit, DiscordConfig.MaxGuildsPerBatch, nameof(args.Limit));
            Preconditions.GreaterThan(args.AfterGuildId, 0, nameof(args.AfterGuildId));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(int.MaxValue);
            ulong afterGuildId = args.AfterGuildId.GetValueOrDefault(0);

            return await SendAsync<IReadOnlyCollection<UserGuild>>("GET", () => $"users/@me/guilds?limit={limit}&after={afterGuildId}", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<Application> GetMyApplicationAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<Application>("GET", () => "oauth2/applications/@me", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<User> ModifySelfAsync(Rest.ModifyCurrentUserParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNullOrEmpty(args.Username, nameof(args.Username));
            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<User>("PATCH", () => "users/@me", args, new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task ModifyMyNickAsync(ulong guildId, Rest.ModifyCurrentUserNickParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNull(args.Nickname, nameof(args.Nickname));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            await SendJsonAsync("PATCH", () => $"guilds/{guildId}/members/@me/nick", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Channel> CreateDMChannelAsync(CreateDMChannelParams args, RequestOptions options = null)
        {
            Preconditions.NotNull(args, nameof(args));
            Preconditions.GreaterThan(args.RecipientId, 0, nameof(args.RecipientId));
            options = RequestOptions.CreateOrClone(options);

            return await SendJsonAsync<Channel>("POST", () => "users/@me/channels", args, new BucketIds(), options: options).ConfigureAwait(false);
        }

        //Voice Regions
        public async Task<IReadOnlyCollection<VoiceRegion>> GetVoiceRegionsAsync(RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            return await SendAsync<IReadOnlyCollection<VoiceRegion>>("GET", () => "voice/regions", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<VoiceRegion>> GetGuildVoiceRegionsAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<VoiceRegion>>("GET", () => $"guilds/{guildId}/regions", ids, options: options).ConfigureAwait(false);
        }

        //Audit logs
        public async Task<AuditLog> GetAuditLogsAsync(ulong guildId, GetAuditLogsParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            Preconditions.NotNull(args, nameof(args));
            options = RequestOptions.CreateOrClone(options);

            int limit = args.Limit.GetValueOrDefault(int.MaxValue);

            var ids = new BucketIds(guildId: guildId);
            Expression<Func<string>> endpoint;

            var queryArgs = new StringBuilder();
            if (args.BeforeEntryId.IsSpecified)
            {
                queryArgs.Append("&before=")
                    .Append(args.BeforeEntryId);
            }
            if (args.UserId.IsSpecified)
            {
                queryArgs.Append("&user_id=")
                    .Append(args.UserId.Value);
            }
            if (args.ActionType.IsSpecified)
            {
                queryArgs.Append("&action_type=")
                    .Append(args.ActionType.Value);
            }

            // still use string interp for the query w/o params, as this is necessary for CreateBucketId
            endpoint = () => $"guilds/{guildId}/audit-logs?limit={limit}{queryArgs.ToString()}";
            return await SendAsync<AuditLog>("GET", endpoint, ids, options: options).ConfigureAwait(false);
        }

        //Webhooks
        public async Task<Webhook> CreateWebhookAsync(ulong channelId, CreateWebhookParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNull(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);
            var ids = new BucketIds(channelId: channelId);

            return await SendJsonAsync<Webhook>("POST", () => $"channels/{channelId}/webhooks", args, ids, options: options).ConfigureAwait(false);
        }
        public async Task<Webhook> GetWebhookAsync(ulong webhookId, RequestOptions options = null)
        {
            Preconditions.NotEqual(webhookId, 0, nameof(webhookId));
            options = RequestOptions.CreateOrClone(options);

            try
            {
                if (AuthTokenType == TokenType.Webhook)
                    return await SendAsync<Webhook>("GET", () => $"webhooks/{webhookId}/{AuthToken}", new BucketIds(), options: options).ConfigureAwait(false);
                else
                    return await SendAsync<Webhook>("GET", () => $"webhooks/{webhookId}", new BucketIds(), options: options).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound) { return null; }
        }
        public async Task<Webhook> ModifyWebhookAsync(ulong webhookId, ModifyWebhookParams args, RequestOptions options = null)
        {
            Preconditions.NotEqual(webhookId, 0, nameof(webhookId));
            Preconditions.NotNull(args, nameof(args));
            Preconditions.NotNullOrEmpty(args.Name, nameof(args.Name));
            options = RequestOptions.CreateOrClone(options);

            if (AuthTokenType == TokenType.Webhook)
                return await SendJsonAsync<Webhook>("PATCH", () => $"webhooks/{webhookId}/{AuthToken}", args, new BucketIds(), options: options).ConfigureAwait(false);
            else
                return await SendJsonAsync<Webhook>("PATCH", () => $"webhooks/{webhookId}", args, new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task DeleteWebhookAsync(ulong webhookId, RequestOptions options = null)
        {
            Preconditions.NotEqual(webhookId, 0, nameof(webhookId));
            options = RequestOptions.CreateOrClone(options);

            if (AuthTokenType == TokenType.Webhook)
                await SendAsync("DELETE", () => $"webhooks/{webhookId}/{AuthToken}", new BucketIds(), options: options).ConfigureAwait(false);
            else
                await SendAsync("DELETE", () => $"webhooks/{webhookId}", new BucketIds(), options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Webhook>> GetGuildWebhooksAsync(ulong guildId, RequestOptions options = null)
        {
            Preconditions.NotEqual(guildId, 0, nameof(guildId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(guildId: guildId);
            return await SendAsync<IReadOnlyCollection<Webhook>>("GET", () => $"guilds/{guildId}/webhooks", ids, options: options).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Webhook>> GetChannelWebhooksAsync(ulong channelId, RequestOptions options = null)
        {
            Preconditions.NotEqual(channelId, 0, nameof(channelId));
            options = RequestOptions.CreateOrClone(options);

            var ids = new BucketIds(channelId: channelId);
            return await SendAsync<IReadOnlyCollection<Webhook>>("GET", () => $"channels/{channelId}/webhooks", ids, options: options).ConfigureAwait(false);
        }

        //Helpers
        /// <exception cref="InvalidOperationException">Client is not logged in.</exception>
        protected void CheckState()
        {
            if (LoginState != LoginState.LoggedIn)
                throw new InvalidOperationException("Client is not logged in.");
        }
        protected static double ToMilliseconds(Stopwatch stopwatch) => Math.Round((double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0, 2);
        protected string SerializeJson(object value)
        {
            var sb = new StringBuilder(256);
            using (TextWriter text = new StringWriter(sb, CultureInfo.InvariantCulture))
            using (JsonWriter writer = new JsonTextWriter(text))
                _serializer.Serialize(writer, value);
            return sb.ToString();
        }
        protected T DeserializeJson<T>(Stream jsonStream)
        {
            using (TextReader text = new StreamReader(jsonStream))
            using (JsonReader reader = new JsonTextReader(text))
                return _serializer.Deserialize<T>(reader);
        }

        internal class BucketIds
        {
            public ulong GuildId { get; internal set; }
            public ulong ChannelId { get; internal set; }
            public ulong WebhookId { get; internal set; }
            public string HttpMethod { get; internal set; }

            internal BucketIds(ulong guildId = 0, ulong channelId = 0, ulong webhookId = 0)
            {
                GuildId = guildId;
                ChannelId = channelId;
                WebhookId = webhookId;
            }

            internal object[] ToArray()
                => new object[] { HttpMethod, GuildId, ChannelId, WebhookId };

            internal Dictionary<string, string> ToMajorParametersDictionary()
            {
                var dict = new Dictionary<string, string>();
                if (GuildId != 0)
                    dict["GuildId"] = GuildId.ToString();
                if (ChannelId != 0)
                    dict["ChannelId"] = ChannelId.ToString();
                if (WebhookId != 0)
                    dict["WebhookId"] = WebhookId.ToString();
                return dict;
            }

            internal static int? GetIndex(string name)
            {
                switch (name)
                {
                    case "httpMethod": return 0;
                    case "guildId": return 1;
                    case "channelId": return 2;
                    case "webhookId": return 3;
                    default:
                        return null;
                }
            }
        }

        private static string GetEndpoint(Expression<Func<string>> endpointExpr)
        {
            return endpointExpr.Compile()();
        }
        private static BucketId GetBucketId(string httpMethod, BucketIds ids, Expression<Func<string>> endpointExpr, string callingMethod)
        {
            ids.HttpMethod ??= httpMethod;
            return _bucketIdGenerators.GetOrAdd(callingMethod, x => CreateBucketId(endpointExpr))(ids);
        }

        private static Func<BucketIds, BucketId> CreateBucketId(Expression<Func<string>> endpoint)
        {
            try
            {
                //Is this a constant string?
                if (endpoint.Body.NodeType == ExpressionType.Constant)
                    return x => BucketId.Create(x.HttpMethod, (endpoint.Body as ConstantExpression).Value.ToString(), x.ToMajorParametersDictionary());

                var builder = new StringBuilder();
                var methodCall = endpoint.Body as MethodCallExpression;
                var methodArgs = methodCall.Arguments.ToArray();
                string format = (methodArgs[0] as ConstantExpression).Value as string;

                //Unpack the array, if one exists (happens with 4+ parameters)
                if (methodArgs.Length > 1 && methodArgs[1].NodeType == ExpressionType.NewArrayInit)
                {
                    var arrayExpr = methodArgs[1] as NewArrayExpression;
                    var elements = arrayExpr.Expressions.ToArray();
                    Array.Resize(ref methodArgs, elements.Length + 1);
                    Array.Copy(elements, 0, methodArgs, 1, elements.Length);
                }

                int endIndex = format.IndexOf('?'); //Dont include params
                if (endIndex == -1)
                    endIndex = format.Length;

                int lastIndex = 0;
                while (true)
                {
                    int leftIndex = format.IndexOf("{", lastIndex);
                    if (leftIndex == -1 || leftIndex > endIndex)
                    {
                        builder.Append(format, lastIndex, endIndex - lastIndex);
                        break;
                    }
                    builder.Append(format, lastIndex, leftIndex - lastIndex);
                    int rightIndex = format.IndexOf("}", leftIndex);

                    int argId = int.Parse(format.Substring(leftIndex + 1, rightIndex - leftIndex - 1), NumberStyles.None, CultureInfo.InvariantCulture);
                    string fieldName = GetFieldName(methodArgs[argId + 1]);

                    var mappedId = BucketIds.GetIndex(fieldName);

                    if (!mappedId.HasValue && rightIndex != endIndex && format.Length > rightIndex + 1 && format[rightIndex + 1] == '/') //Ignore the next slash
                        rightIndex++;

                    if (mappedId.HasValue)
                        builder.Append($"{{{mappedId.Value}}}");

                    lastIndex = rightIndex + 1;
                }
                if (builder[builder.Length - 1] == '/')
                    builder.Remove(builder.Length - 1, 1);

                format = builder.ToString();

                return x => BucketId.Create(x.HttpMethod, string.Format(format, x.ToArray()), x.ToMajorParametersDictionary());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate the bucket id for this operation.", ex);
            }
        }

        private static string GetFieldName(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Convert)
                expr = (expr as UnaryExpression).Operand;

            if (expr.NodeType != ExpressionType.MemberAccess)
                throw new InvalidOperationException("Unsupported expression");

            return (expr as MemberExpression).Member.Name;
        }
    }
}
