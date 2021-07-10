#pragma warning disable CS1591
using Discord.API.Gateway;
using Discord.Net.Queue;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameModel = Discord.API.Game;

namespace Discord.API
{
    internal class DiscordSocketApiClient : DiscordRestApiClient
    {
        public event Func<GatewayOpCode, Task> SentGatewayMessage { add { _sentGatewayMessageEvent.Add(value); } remove { _sentGatewayMessageEvent.Remove(value); } }
        private readonly AsyncEvent<Func<GatewayOpCode, Task>> _sentGatewayMessageEvent = new AsyncEvent<Func<GatewayOpCode, Task>>();
        public event Func<GatewayOpCode, int?, string, object, Task> ReceivedGatewayEvent { add { _receivedGatewayEvent.Add(value); } remove { _receivedGatewayEvent.Remove(value); } }
        private readonly AsyncEvent<Func<GatewayOpCode, int?, string, object, Task>> _receivedGatewayEvent = new AsyncEvent<Func<GatewayOpCode, int?, string, object, Task>>();

        public event Func<Exception, Task> Disconnected { add { _disconnectedEvent.Add(value); } remove { _disconnectedEvent.Remove(value); } }
        private readonly AsyncEvent<Func<Exception, Task>> _disconnectedEvent = new AsyncEvent<Func<Exception, Task>>();

        private readonly bool _isExplicitUrl;
        private CancellationTokenSource _connectCancelToken;
        private string _gatewayUrl;

        //Store our decompression streams for zlib shared state
        private MemoryStream _compressed;
        private DeflateStream _decompressor;

        internal IWebSocketClient WebSocketClient { get; }

        public ConnectionState ConnectionState { get; private set; }

        public DiscordSocketApiClient(RestClientProvider restClientProvider, WebSocketProvider webSocketProvider, string userAgent,
            string url = null, RetryMode defaultRetryMode = RetryMode.AlwaysRetry, JsonSerializer serializer = null,
			bool useSystemClock = true)
            : base(restClientProvider, userAgent, defaultRetryMode, serializer, useSystemClock)
        {
            _gatewayUrl = url;
            if (url != null)
                _isExplicitUrl = true;
            WebSocketClient = webSocketProvider();
            //WebSocketClient.SetHeader("user-agent", DiscordConfig.UserAgent); (Causes issues in .NET Framework 4.6+)

            WebSocketClient.BinaryMessage += async (data, index, count) =>
            {
                using (var decompressed = new MemoryStream())
                {
                    if (data[0] == 0x78)
                    {
                        //Strip the zlib header
                        _compressed.Write(data, index + 2, count - 2);
                        _compressed.SetLength(count - 2);
                    }
                    else
                    {
                        _compressed.Write(data, index, count);
                        _compressed.SetLength(count);
                    }

                    //Reset positions so we don't run out of memory
                    _compressed.Position = 0;
                    _decompressor.CopyTo(decompressed);
                    _compressed.Position = 0;
                    decompressed.Position = 0;

                    using (var reader = new StreamReader(decompressed))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        var msg = _serializer.Deserialize<SocketFrame>(jsonReader);
                        if (msg != null)
                            await _receivedGatewayEvent.InvokeAsync((GatewayOpCode)msg.Operation, msg.Sequence, msg.Type, msg.Payload).ConfigureAwait(false);
                    }
                }
            };
            WebSocketClient.TextMessage += async text =>
            {
                using (var reader = new StringReader(text))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var msg = _serializer.Deserialize<SocketFrame>(jsonReader);
                    if (msg != null)
                        await _receivedGatewayEvent.InvokeAsync((GatewayOpCode)msg.Operation, msg.Sequence, msg.Type, msg.Payload).ConfigureAwait(false);
                }
            };
            WebSocketClient.Closed += async ex =>
            {
                await DisconnectAsync().ConfigureAwait(false);
                await _disconnectedEvent.InvokeAsync(ex).ConfigureAwait(false);
            };
        }

        internal override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _connectCancelToken?.Dispose();
                    (WebSocketClient as IDisposable)?.Dispose();
                    _decompressor?.Dispose();
                    _compressed?.Dispose();
                }
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        public async Task ConnectAsync()
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await ConnectInternalAsync().ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        /// <exception cref="InvalidOperationException">The client must be logged in before connecting.</exception>
        /// <exception cref="NotSupportedException">This client is not configured with WebSocket support.</exception>
        internal override async Task ConnectInternalAsync()
        {
            if (LoginState != LoginState.LoggedIn)
                throw new InvalidOperationException("The client must be logged in before connecting.");
            if (WebSocketClient == null)
                throw new NotSupportedException("This client is not configured with WebSocket support.");

            RequestQueue.ClearGatewayBuckets();

            //Re-create streams to reset the zlib state
            _compressed?.Dispose();
            _decompressor?.Dispose();
            _compressed = new MemoryStream();
            _decompressor = new DeflateStream(_compressed, CompressionMode.Decompress);

            ConnectionState = ConnectionState.Connecting;
            try
            {
                _connectCancelToken?.Dispose();
                _connectCancelToken = new CancellationTokenSource();
                if (WebSocketClient != null)
                    WebSocketClient.SetCancelToken(_connectCancelToken.Token);

                if (!_isExplicitUrl)
                {
                    var gatewayResponse = await GetGatewayAsync().ConfigureAwait(false);
                    _gatewayUrl = $"{gatewayResponse.Url}?v={DiscordConfig.APIVersion}&encoding={DiscordSocketConfig.GatewayEncoding}&compress=zlib-stream";
                }
                await WebSocketClient.ConnectAsync(_gatewayUrl).ConfigureAwait(false);

                ConnectionState = ConnectionState.Connected;
            }
            catch
            {
                if (!_isExplicitUrl)
                    _gatewayUrl = null; //Uncache in case the gateway url changed
                await DisconnectInternalAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task DisconnectAsync(Exception ex = null)
        {
            await _stateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectInternalAsync(ex).ConfigureAwait(false);
            }
            finally { _stateLock.Release(); }
        }
        /// <exception cref="NotSupportedException">This client is not configured with WebSocket support.</exception>
        internal override async Task DisconnectInternalAsync(Exception ex = null)
        {
            if (WebSocketClient == null)
                throw new NotSupportedException("This client is not configured with WebSocket support.");

            if (ConnectionState == ConnectionState.Disconnected) return;
            ConnectionState = ConnectionState.Disconnecting;

            try { _connectCancelToken?.Cancel(false); }
            catch { }

            if (ex is GatewayReconnectException)
                await WebSocketClient.DisconnectAsync(4000).ConfigureAwait(false);
            else
                await WebSocketClient.DisconnectAsync().ConfigureAwait(false);

            ConnectionState = ConnectionState.Disconnected;
        }

        //Core
        public Task SendGatewayAsync(GatewayOpCode opCode, object payload, RequestOptions options = null)
            => SendGatewayInternalAsync(opCode, payload, options);
        private async Task SendGatewayInternalAsync(GatewayOpCode opCode, object payload, RequestOptions options)
        {
            CheckState();

            //TODO: Add ETF
            byte[] bytes = null;
            payload = new SocketFrame { Operation = (int)opCode, Payload = payload };
            if (payload != null)
                bytes = Encoding.UTF8.GetBytes(SerializeJson(payload));

            options.IsGatewayBucket = true;
            if (options.BucketId == null)
                options.BucketId = GatewayBucket.Get(GatewayBucketType.Unbucketed).Id;
            await RequestQueue.SendAsync(new WebSocketRequest(WebSocketClient, bytes, true, opCode == GatewayOpCode.Heartbeat, options)).ConfigureAwait(false);
            await _sentGatewayMessageEvent.InvokeAsync(opCode).ConfigureAwait(false);
        }

        public async Task SendIdentifyAsync(int largeThreshold = 100, int shardID = 0, int totalShards = 1, GatewayIntents gatewayIntents = GatewayIntents.AllUnprivileged, (UserStatus, bool, long?, GameModel)? presence = null, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            var props = new Dictionary<string, string>
            {
                ["$device"] = "Discord.Net"
            };
            var msg = new IdentifyParams()
            {
                Token = AuthToken,
                Properties = props,
                LargeThreshold = largeThreshold
            };
            if (totalShards > 1)
                msg.ShardingParams = new int[] { shardID, totalShards };

            options.BucketId = GatewayBucket.Get(GatewayBucketType.Identify).Id;

            msg.Intents = (int)gatewayIntents;

            if (presence.HasValue)
            {
                msg.Presence = new StatusUpdateParams
                {
                    Status = presence.Value.Item1,
                    IsAFK = presence.Value.Item2,
                    IdleSince = presence.Value.Item3,
                    Game = presence.Value.Item4,
                };
            }

            await SendGatewayAsync(GatewayOpCode.Identify, msg, options: options).ConfigureAwait(false);
        }
        public async Task SendResumeAsync(string sessionId, int lastSeq, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            var msg = new ResumeParams()
            {
                Token = AuthToken,
                SessionId = sessionId,
                Sequence = lastSeq
            };
            await SendGatewayAsync(GatewayOpCode.Resume, msg, options: options).ConfigureAwait(false);
        }
        public async Task SendHeartbeatAsync(int lastSeq, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            await SendGatewayAsync(GatewayOpCode.Heartbeat, lastSeq, options: options).ConfigureAwait(false);
        }
        public async Task SendStatusUpdateAsync(UserStatus status, bool isAFK, long? since, GameModel game, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            var args = new StatusUpdateParams
            {
                Status = status,
                IdleSince = since,
                IsAFK = isAFK,
                Game = game
            };
            options.BucketId = GatewayBucket.Get(GatewayBucketType.PresenceUpdate).Id;
            await SendGatewayAsync(GatewayOpCode.StatusUpdate, args, options: options).ConfigureAwait(false);
        }
        public async Task SendRequestMembersAsync(IEnumerable<ulong> guildIds, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            await SendGatewayAsync(GatewayOpCode.RequestGuildMembers, new RequestMembersParams { GuildIds = guildIds, Query = "", Limit = 0 }, options: options).ConfigureAwait(false);
        }
        public async Task SendVoiceStateUpdateAsync(ulong guildId, ulong? channelId, bool selfDeaf, bool selfMute, RequestOptions options = null)
        {
            var payload = new VoiceStateUpdateParams
            {
                GuildId = guildId,
                ChannelId = channelId,
                SelfDeaf = selfDeaf,
                SelfMute = selfMute
            };
            await SendGatewayAsync(GatewayOpCode.VoiceStateUpdate, payload, options: options).ConfigureAwait(false);
        }
        public async Task SendVoiceStateUpdateAsync(VoiceStateUpdateParams payload, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            await SendGatewayAsync(GatewayOpCode.VoiceStateUpdate, payload, options: options).ConfigureAwait(false);
        }
        public async Task SendGuildSyncAsync(IEnumerable<ulong> guildIds, RequestOptions options = null)
        {
            options = RequestOptions.CreateOrClone(options);
            await SendGatewayAsync(GatewayOpCode.GuildSync, guildIds, options: options).ConfigureAwait(false);
        }
    }
}
