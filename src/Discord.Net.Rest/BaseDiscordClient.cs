using Discord.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Rest
{
    public abstract class BaseDiscordClient : IDiscordClient
    {
        public event Func<LogMessage, Task> Log { add { _logEvent.Add(value); } remove { _logEvent.Remove(value); } }
        internal readonly AsyncEvent<Func<LogMessage, Task>> _logEvent = new AsyncEvent<Func<LogMessage, Task>>();

        public event Func<Task> LoggedIn { add { _loggedInEvent.Add(value); } remove { _loggedInEvent.Remove(value); } }
        private readonly AsyncEvent<Func<Task>> _loggedInEvent = new AsyncEvent<Func<Task>>();
        public event Func<Task> LoggedOut { add { _loggedOutEvent.Add(value); } remove { _loggedOutEvent.Remove(value); } }
        private readonly AsyncEvent<Func<Task>> _loggedOutEvent = new AsyncEvent<Func<Task>>();

        internal readonly Logger _restLogger;
        private readonly SemaphoreSlim _stateLock;
        private bool _isFirstLogin, _isDisposed;

        internal API.DiscordRestApiClient ApiClient { get; }
        internal LogManager LogManager { get; }
        /// <summary>
        ///     Gets the login state of the client.
        /// </summary>
        public LoginState LoginState { get; private set; }
        /// <summary>
        ///     Gets the logged-in user.
        /// </summary>
        public ISelfUser CurrentUser { get; protected set; }
        /// <inheritdoc />
        public TokenType TokenType => ApiClient.AuthTokenType;

        /// <summary> Creates a new REST-only Discord client. </summary>
        internal BaseDiscordClient(DiscordRestConfig config, API.DiscordRestApiClient client)
        {
            ApiClient = client;
            LogManager = new LogManager(config.LogLevel);
            LogManager.Message += async msg => await _logEvent.InvokeAsync(msg).ConfigureAwait(false);

            _stateLock = new SemaphoreSlim(1, 1);
            _restLogger = LogManager.CreateLogger("Rest");
            _isFirstLogin = config.DisplayInitialLog;

            ApiClient.RequestQueue.RateLimitTriggered += async (id, info, endpoint) =>
            {
                if (info == null)
                    await _restLogger.VerboseAsync($"Preemptive Rate limit triggered: {endpoint} {(id.IsHashBucket ? $"(Bucket: {id.BucketHash})" : "")}").ConfigureAwait(false);
                else
                    await _restLogger.WarningAsync($"Rate limit triggered: {endpoint} {(id.IsHashBucket ? $"(Bucket: {id.BucketHash})" : "")}").ConfigureAwait(false);
            };
            ApiClient.SentRequest += async (method, endpoint, millis) => await _restLogger.VerboseAsync($"{method} {endpoint}: {millis} ms").ConfigureAwait(false);
        }

        public async Task LoginAsync(TokenType tokenType, string token, bool validateToken = true)
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LoginInternalAsync(tokenType, token, validateToken).ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        internal virtual async Task LoginInternalAsync(TokenType tokenType, string token, bool validateToken)
        {
            if (_isFirstLogin)
            {
                _isFirstLogin = false;
                await LogManager.WriteInitialLog().ConfigureAwait(false);
            }

            if (LoginState != LoginState.LoggedOut)
                await LogoutInternalAsync().ConfigureAwait(false);
            LoginState = LoginState.LoggingIn;

            try
            {
                // If token validation is enabled, validate the token and let it throw any ArgumentExceptions
                // that result from invalid parameters
                if (validateToken)
                {
                    try
                    {
                        TokenUtils.ValidateToken(tokenType, token);
                    }
                    catch (ArgumentException ex)
                    {
                        // log these ArgumentExceptions and allow for the client to attempt to log in anyways
                        await LogManager.WarningAsync("Discord", "A supplied token was invalid.", ex).ConfigureAwait(false);
                    }
                }

                await ApiClient.LoginAsync(tokenType, token).ConfigureAwait(false);
                await OnLoginAsync(tokenType, token).ConfigureAwait(false);
                LoginState = LoginState.LoggedIn;
            }
            catch
            {
                await LogoutInternalAsync().ConfigureAwait(false);
                throw;
            }

            await _loggedInEvent.InvokeAsync().ConfigureAwait(false);
        }
        internal virtual Task OnLoginAsync(TokenType tokenType, string token)
            => Task.Delay(0);

        public async Task LogoutAsync()
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LogoutInternalAsync().ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        internal virtual async Task LogoutInternalAsync()
        {
            if (LoginState == LoginState.LoggedOut) return;
            LoginState = LoginState.LoggingOut;

            await ApiClient.LogoutAsync().ConfigureAwait(false);

            await OnLogoutAsync().ConfigureAwait(false);
            CurrentUser = null;
            LoginState = LoginState.LoggedOut;

            await _loggedOutEvent.InvokeAsync().ConfigureAwait(false);
        }
        internal virtual Task OnLogoutAsync()
            => Task.Delay(0);

        internal virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
#pragma warning disable IDISP007
                ApiClient.Dispose();
#pragma warning restore IDISP007
                _stateLock?.Dispose();
                _isDisposed = true;
            }
        }

        internal virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_isDisposed)
            {
#pragma warning disable IDISP007
                await ApiClient.DisposeAsync().ConfigureAwait(false);
#pragma warning restore IDISP007
                _stateLock?.Dispose();
                _isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        public ValueTask DisposeAsync() => DisposeAsync(true);

        /// <inheritdoc />
        public Task<int> GetRecommendedShardCountAsync(RequestOptions options = null)
            => ClientHelper.GetRecommendShardCountAsync(this, options);

        /// <inheritdoc />
        public Task<BotGateway> GetBotGatewayAsync(RequestOptions options = null)
            => ClientHelper.GetBotGatewayAsync(this, options);

        //IDiscordClient
        /// <inheritdoc />
        ConnectionState IDiscordClient.ConnectionState => ConnectionState.Disconnected;
        /// <inheritdoc />
        ISelfUser IDiscordClient.CurrentUser => CurrentUser;

        /// <inheritdoc />
        Task<IApplication> IDiscordClient.GetApplicationInfoAsync(RequestOptions options)
            => throw new NotSupportedException();

        /// <inheritdoc />
        Task<IChannel> IDiscordClient.GetChannelAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IChannel>(null);
        /// <inheritdoc />
        Task<IReadOnlyCollection<IPrivateChannel>> IDiscordClient.GetPrivateChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IPrivateChannel>>(ImmutableArray.Create<IPrivateChannel>());
        /// <inheritdoc />
        Task<IReadOnlyCollection<IDMChannel>> IDiscordClient.GetDMChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IDMChannel>>(ImmutableArray.Create<IDMChannel>());
        /// <inheritdoc />
        Task<IReadOnlyCollection<IGroupChannel>> IDiscordClient.GetGroupChannelsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IGroupChannel>>(ImmutableArray.Create<IGroupChannel>());

        /// <inheritdoc />
        Task<IReadOnlyCollection<IConnection>> IDiscordClient.GetConnectionsAsync(RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IConnection>>(ImmutableArray.Create<IConnection>());

        /// <inheritdoc />
        Task<IInvite> IDiscordClient.GetInviteAsync(string inviteId, RequestOptions options)
            => Task.FromResult<IInvite>(null);

        /// <inheritdoc />
        Task<IGuild> IDiscordClient.GetGuildAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IGuild>(null);
        /// <inheritdoc />
        Task<IReadOnlyCollection<IGuild>> IDiscordClient.GetGuildsAsync(CacheMode mode, RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IGuild>>(ImmutableArray.Create<IGuild>());
        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Creating a guild is not supported with the base client.</exception>
        Task<IGuild> IDiscordClient.CreateGuildAsync(string name, IVoiceRegion region, Stream jpegIcon, RequestOptions options)
            => throw new NotSupportedException();

        /// <inheritdoc />
        Task<IUser> IDiscordClient.GetUserAsync(ulong id, CacheMode mode, RequestOptions options)
            => Task.FromResult<IUser>(null);
        /// <inheritdoc />
        Task<IUser> IDiscordClient.GetUserAsync(string username, string discriminator, RequestOptions options)
            => Task.FromResult<IUser>(null);

        /// <inheritdoc />
        Task<IReadOnlyCollection<IVoiceRegion>> IDiscordClient.GetVoiceRegionsAsync(RequestOptions options)
            => Task.FromResult<IReadOnlyCollection<IVoiceRegion>>(ImmutableArray.Create<IVoiceRegion>());
        /// <inheritdoc />
        Task<IVoiceRegion> IDiscordClient.GetVoiceRegionAsync(string id, RequestOptions options)
            => Task.FromResult<IVoiceRegion>(null);

        /// <inheritdoc />
        Task<IWebhook> IDiscordClient.GetWebhookAsync(ulong id, RequestOptions options)
            => Task.FromResult<IWebhook>(null);

        /// <inheritdoc />
        Task IDiscordClient.StartAsync()
            => Task.Delay(0);
        /// <inheritdoc />
        Task IDiscordClient.StopAsync()
            => Task.Delay(0);
    }
}
