using Discord.API.Voice;
using Discord.Audio.Streams;
using Discord.Logging;
using Discord.Net.Converters;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Discord.Audio
{
    //TODO: Add audio reconnecting
    internal partial class AudioClient : IAudioClient
    {
        internal struct StreamPair
        {
            public AudioInStream Reader;
            public AudioOutStream Writer;

            public StreamPair(AudioInStream reader, AudioOutStream writer)
            {
                Reader = reader;
                Writer = writer;
            }
        }

        private readonly Logger _audioLogger;
        private readonly JsonSerializer _serializer;
        private readonly ConnectionManager _connection;
        private readonly SemaphoreSlim _stateLock;
        private readonly ConcurrentQueue<long> _heartbeatTimes;
        private readonly ConcurrentQueue<KeyValuePair<ulong, int>> _keepaliveTimes;
        private readonly ConcurrentDictionary<uint, ulong> _ssrcMap;
        private readonly ConcurrentDictionary<ulong, StreamPair> _streams;

        private int _heartbeatInterval;
        private Task _heartbeatTask, _keepaliveTask;
        private long _lastMessageTime;
        private string _url, _sessionId, _token;
        private ulong _userId;
        private uint _ssrc;
        private bool _isSpeaking;

        public SocketGuild Guild { get; }
        public DiscordVoiceAPIClient ApiClient { get; private set; }
        public int Latency { get; private set; }
        public int UdpLatency { get; private set; }
        public ulong ChannelId { get; internal set; }
        internal byte[] SecretKey { get; private set; }

        private DiscordSocketClient Discord => Guild.Discord;
        public ConnectionState ConnectionState => _connection.State;

        /// <summary> Creates a new REST/WebSocket discord client. </summary>
        internal AudioClient(SocketGuild guild, int clientId, ulong channelId)
        {
            Guild = guild;
            ChannelId = channelId;
            _audioLogger = Discord.LogManager.CreateLogger($"Audio #{clientId}");

            ApiClient = new DiscordVoiceAPIClient(guild.Id, Discord.WebSocketProvider, Discord.UdpSocketProvider);
            ApiClient.SentGatewayMessage += async opCode => await _audioLogger.DebugAsync($"Sent {opCode}").ConfigureAwait(false);
            ApiClient.SentDiscovery += async () => await _audioLogger.DebugAsync("Sent Discovery").ConfigureAwait(false);
            //ApiClient.SentData += async bytes => await _audioLogger.DebugAsync($"Sent {bytes} Bytes").ConfigureAwait(false);
            ApiClient.ReceivedEvent += ProcessMessageAsync;
            ApiClient.ReceivedPacket += ProcessPacketAsync;

            _stateLock = new SemaphoreSlim(1, 1);
            _connection = new ConnectionManager(_stateLock, _audioLogger, 30000,
                OnConnectingAsync, OnDisconnectingAsync, x => ApiClient.Disconnected += x);
            _connection.Connected += () => _connectedEvent.InvokeAsync();
            _connection.Disconnected += (ex, recon) => _disconnectedEvent.InvokeAsync(ex);
            _heartbeatInterval = 41250;
            _heartbeatTimes = new ConcurrentQueue<long>();
            _keepaliveTimes = new ConcurrentQueue<KeyValuePair<ulong, int>>();
            _ssrcMap = new ConcurrentDictionary<uint, ulong>();
            _streams = new ConcurrentDictionary<ulong, StreamPair>();

            _serializer = new JsonSerializer { ContractResolver = new DiscordContractResolver() };
            _serializer.Error += (s, e) =>
            {
                _audioLogger.WarningAsync(e.ErrorContext.Error).GetAwaiter().GetResult();
                e.ErrorContext.Handled = true;
            };

            LatencyUpdated += async (old, val) => await _audioLogger.DebugAsync($"Latency = {val} ms").ConfigureAwait(false);
            UdpLatencyUpdated += async (old, val) => await _audioLogger.DebugAsync($"UDP Latency = {val} ms").ConfigureAwait(false);
        }

        internal async Task StartAsync(string url, ulong userId, string sessionId, string token)
        {
            _url = url;
            _userId = userId;
            _sessionId = sessionId;
            _token = token;
            await _connection.StartAsync().ConfigureAwait(false);
        }

        public IReadOnlyDictionary<ulong, AudioInStream> GetStreams()
        {
            return _streams.ToDictionary(pair => pair.Key, pair => pair.Value.Reader);
        }

        public async Task StopAsync()
        {
            await _connection.StopAsync().ConfigureAwait(false);
        }

        private async Task OnConnectingAsync()
        {
            await _audioLogger.DebugAsync("Connecting ApiClient").ConfigureAwait(false);
            await ApiClient.ConnectAsync("wss://" + _url + "?v=" + DiscordConfig.VoiceAPIVersion).ConfigureAwait(false);
            await _audioLogger.DebugAsync("Listening on port " + ApiClient.UdpPort).ConfigureAwait(false);
            await _audioLogger.DebugAsync("Sending Identity").ConfigureAwait(false);
            await ApiClient.SendIdentityAsync(_userId, _sessionId, _token).ConfigureAwait(false);

            //Wait for READY
            await _connection.WaitAsync().ConfigureAwait(false);
        }
        private async Task OnDisconnectingAsync(Exception ex)
        {
            await _audioLogger.DebugAsync("Disconnecting ApiClient").ConfigureAwait(false);
            await ApiClient.DisconnectAsync().ConfigureAwait(false);

            //Wait for tasks to complete
            await _audioLogger.DebugAsync("Waiting for heartbeater").ConfigureAwait(false);
            var heartbeatTask = _heartbeatTask;
            if (heartbeatTask != null)
                await heartbeatTask.ConfigureAwait(false);
            _heartbeatTask = null;
            var keepaliveTask = _keepaliveTask;
            if (keepaliveTask != null)
                await keepaliveTask.ConfigureAwait(false);
            _keepaliveTask = null;

            while (_heartbeatTimes.TryDequeue(out _)) { }
            _lastMessageTime = 0;

            await ClearInputStreamsAsync().ConfigureAwait(false);

            await _audioLogger.DebugAsync("Sending Voice State").ConfigureAwait(false);
            await Discord.ApiClient.SendVoiceStateUpdateAsync(Guild.Id, null, false, false).ConfigureAwait(false);
        }

        public AudioOutStream CreateOpusStream(int bufferMillis)
        {
            var outputStream = new OutputStream(ApiClient); //Ignores header
            var sodiumEncrypter = new SodiumEncryptStream( outputStream, this); //Passes header
            var rtpWriter = new RTPWriteStream(sodiumEncrypter, _ssrc); //Consumes header, passes
            return new BufferedWriteStream(rtpWriter, this, bufferMillis, _connection.CancelToken, _audioLogger); //Generates header
        }
        public AudioOutStream CreateDirectOpusStream()
        {
            var outputStream = new OutputStream(ApiClient); //Ignores header
            var sodiumEncrypter = new SodiumEncryptStream(outputStream, this); //Passes header
            return new RTPWriteStream(sodiumEncrypter, _ssrc); //Consumes header (external input), passes
        }
        public AudioOutStream CreatePCMStream(AudioApplication application, int? bitrate, int bufferMillis, int packetLoss)
        {
            var outputStream = new OutputStream(ApiClient); //Ignores header
            var sodiumEncrypter = new SodiumEncryptStream(outputStream, this); //Passes header
            var rtpWriter = new RTPWriteStream(sodiumEncrypter, _ssrc); //Consumes header, passes
            var bufferedStream = new BufferedWriteStream(rtpWriter, this, bufferMillis, _connection.CancelToken, _audioLogger); //Ignores header, generates header
            return new OpusEncodeStream(bufferedStream, bitrate ?? (96 * 1024), application, packetLoss); //Generates header
        }
        public AudioOutStream CreateDirectPCMStream(AudioApplication application, int? bitrate, int packetLoss)
        {
            var outputStream = new OutputStream(ApiClient); //Ignores header
            var sodiumEncrypter = new SodiumEncryptStream(outputStream, this); //Passes header
            var rtpWriter = new RTPWriteStream(sodiumEncrypter, _ssrc); //Consumes header, passes
            return new OpusEncodeStream(rtpWriter, bitrate ?? (96 * 1024), application, packetLoss); //Generates header
        }

        internal async Task CreateInputStreamAsync(ulong userId)
        {
            //Assume Thread-safe
            if (!_streams.ContainsKey(userId))
            {
                var readerStream = new InputStream(); //Consumes header
                var opusDecoder = new OpusDecodeStream(readerStream); //Passes header
                //var jitterBuffer = new JitterBuffer(opusDecoder, _audioLogger);
                var rtpReader = new RTPReadStream(opusDecoder); //Generates header
                var decryptStream = new SodiumDecryptStream(rtpReader, this); //No header
                _streams.TryAdd(userId, new StreamPair(readerStream, decryptStream));
                await _streamCreatedEvent.InvokeAsync(userId, readerStream);
            }
        }
        internal AudioInStream GetInputStream(ulong id)
        {
            if (_streams.TryGetValue(id, out StreamPair streamPair))
                return streamPair.Reader;
            return null;
        }
        internal async Task RemoveInputStreamAsync(ulong userId)
        {
            if (_streams.TryRemove(userId, out var pair))
            {
                await _streamDestroyedEvent.InvokeAsync(userId).ConfigureAwait(false);
                pair.Reader.Dispose();
            }
        }
        internal async Task ClearInputStreamsAsync()
        {
            foreach (var pair in _streams)
            {
                await _streamDestroyedEvent.InvokeAsync(pair.Key).ConfigureAwait(false);
                pair.Value.Reader.Dispose();
            }
            _ssrcMap.Clear();
            _streams.Clear();
        }

        private async Task ProcessMessageAsync(VoiceOpCode opCode, object payload)
        {
            _lastMessageTime = Environment.TickCount;

            try
            {
                switch (opCode)
                {
                    case VoiceOpCode.Hello:
                        {
                            await _audioLogger.DebugAsync("Received Hello").ConfigureAwait(false);
                            var data = (payload as JToken).ToObject<HelloEvent>(_serializer);

                            _heartbeatInterval = data.HeartbeatInterval;
                        }
                        break;
                    case VoiceOpCode.Ready:
                        {
                            await _audioLogger.DebugAsync("Received Ready").ConfigureAwait(false);
                            var data = (payload as JToken).ToObject<ReadyEvent>(_serializer);

                            _ssrc = data.SSRC;

                            if (!data.Modes.Contains(DiscordVoiceAPIClient.Mode))
                                throw new InvalidOperationException($"Discord does not support {DiscordVoiceAPIClient.Mode}");

                            ApiClient.SetUdpEndpoint(data.Ip, data.Port);
                            await ApiClient.SendDiscoveryAsync(_ssrc).ConfigureAwait(false);


                            _heartbeatTask = RunHeartbeatAsync(_heartbeatInterval - 1000, _connection.CancelToken); // reserve a delay time of network
                        }
                        break;
                    case VoiceOpCode.SessionDescription:
                        {
                            await _audioLogger.DebugAsync("Received SessionDescription").ConfigureAwait(false);
                            var data = (payload as JToken).ToObject<SessionDescriptionEvent>(_serializer);

                            if (data.Mode != DiscordVoiceAPIClient.Mode)
                                throw new InvalidOperationException($"Discord selected an unexpected mode: {data.Mode}");

                            SecretKey = data.SecretKey;
                            _isSpeaking = false;
                            await ApiClient.SendSetSpeaking(false).ConfigureAwait(false);
                            _keepaliveTask = RunKeepaliveAsync(5000, _connection.CancelToken);

                            var _ = _connection.CompleteAsync();
                        }
                        break;
                    case VoiceOpCode.HeartbeatAck:
                        {
                            await _audioLogger.DebugAsync("Received HeartbeatAck").ConfigureAwait(false);

                            if (_heartbeatTimes.TryDequeue(out long time))
                            {
                                int latency = (int)(Environment.TickCount - time);
                                int before = Latency;
                                Latency = latency;

                                await _latencyUpdatedEvent.InvokeAsync(before, latency).ConfigureAwait(false);
                            }
                        }
                        break;
                    case VoiceOpCode.Speaking:
                        {
                            await _audioLogger.DebugAsync("Received Speaking").ConfigureAwait(false);

                            var data = (payload as JToken).ToObject<SpeakingEvent>(_serializer);
                            _ssrcMap[data.Ssrc] = data.UserId; //TODO: Memory Leak: SSRCs are never cleaned up

                            await _speakingUpdatedEvent.InvokeAsync(data.UserId, data.Speaking);
                        }
                        break;
                    default:
                        await _audioLogger.WarningAsync($"Unknown OpCode ({opCode})").ConfigureAwait(false);
                        return;
                }
            }
            catch (Exception ex)
            {
                await _audioLogger.ErrorAsync($"Error handling {opCode}", ex).ConfigureAwait(false);
                return;
            }
        }
        private async Task ProcessPacketAsync(byte[] packet)
        {
            try
            {
                if (_connection.State == ConnectionState.Connecting)
                {
                    if (packet.Length != 70)
                    {
                        await _audioLogger.DebugAsync("Malformed Packet").ConfigureAwait(false);
                        return;
                    }
                    string ip;
                    int port;
                    try
                    {
                        ip = Encoding.UTF8.GetString(packet, 4, 70 - 6).TrimEnd('\0');
                        port = (packet[69] << 8) | packet[68];
                    }
                    catch (Exception ex)
                    {
                        await _audioLogger.DebugAsync("Malformed Packet", ex).ConfigureAwait(false);
                        return;
                    }

                    await _audioLogger.DebugAsync("Received Discovery").ConfigureAwait(false);
                    await ApiClient.SendSelectProtocol(ip, port).ConfigureAwait(false);
                }
                else if (_connection.State == ConnectionState.Connected)
                {
                    if (packet.Length == 8)
                    {
                        await _audioLogger.DebugAsync("Received Keepalive").ConfigureAwait(false);

                        ulong value =
                            ((ulong)packet[0] >> 0) |
                            ((ulong)packet[1] >> 8) |
                            ((ulong)packet[2] >> 16) |
                            ((ulong)packet[3] >> 24) |
                            ((ulong)packet[4] >> 32) |
                            ((ulong)packet[5] >> 40) |
                            ((ulong)packet[6] >> 48) |
                            ((ulong)packet[7] >> 56);

                        while (_keepaliveTimes.TryDequeue(out var pair))
                        {
                            if (pair.Key == value)
                            {
                                int latency = Environment.TickCount - pair.Value;
                                int before = UdpLatency;
                                UdpLatency = latency;

                                await _udpLatencyUpdatedEvent.InvokeAsync(before, latency).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (!RTPReadStream.TryReadSsrc(packet, 0, out var ssrc))
                        {
                            await _audioLogger.DebugAsync("Malformed Frame").ConfigureAwait(false);
                            return;
                        }
                        if (!_ssrcMap.TryGetValue(ssrc, out var userId))
                        {
                            await _audioLogger.DebugAsync($"Unknown SSRC {ssrc}").ConfigureAwait(false);
                            return;
                        }
                        if (!_streams.TryGetValue(userId, out var pair))
                        {
                            await _audioLogger.DebugAsync($"Unknown User {userId}").ConfigureAwait(false);
                            return;
                        }
                        try
                        {
                            await pair.Writer.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await _audioLogger.DebugAsync("Malformed Frame", ex).ConfigureAwait(false);
                            return;
                        }
                        //await _audioLogger.DebugAsync($"Received {packet.Length} bytes from user {userId}").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await _audioLogger.WarningAsync("Failed to process UDP packet", ex).ConfigureAwait(false);
                return;
            }
        }

        private async Task RunHeartbeatAsync(int intervalMillis, CancellationToken cancelToken)
        {
            // TODO: Clean this up when Discord's session patch is live
            try
            {
                await _audioLogger.DebugAsync("Heartbeat Started").ConfigureAwait(false);
                while (!cancelToken.IsCancellationRequested)
                {
                    var now = Environment.TickCount;

                    //Did server respond to our last heartbeat?
                    if (_heartbeatTimes.Count != 0 && (now - _lastMessageTime) > intervalMillis &&
                        ConnectionState == ConnectionState.Connected)
                    {
                        _connection.Error(new Exception("Server missed last heartbeat"));
                        return;
                    }

                    _heartbeatTimes.Enqueue(now);
                    try
                    {
                        await ApiClient.SendHeartbeatAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _audioLogger.WarningAsync("Failed to send heartbeat", ex).ConfigureAwait(false);
                    }

                    await Task.Delay(intervalMillis, cancelToken).ConfigureAwait(false);
                }
                await _audioLogger.DebugAsync("Heartbeat Stopped").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await _audioLogger.DebugAsync("Heartbeat Stopped").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _audioLogger.ErrorAsync("Heartbeat Errored", ex).ConfigureAwait(false);
            }
        }
        private async Task RunKeepaliveAsync(int intervalMillis, CancellationToken cancelToken)
        {
            try
            {
                await _audioLogger.DebugAsync("Keepalive Started").ConfigureAwait(false);
                while (!cancelToken.IsCancellationRequested)
                {
                    var now = Environment.TickCount;

                    try
                    {
                        ulong value = await ApiClient.SendKeepaliveAsync().ConfigureAwait(false);
                        if (_keepaliveTimes.Count < 12) //No reply for 60 Seconds
                            _keepaliveTimes.Enqueue(new KeyValuePair<ulong, int>(value, now));
                    }
                    catch (Exception ex)
                    {
                        await _audioLogger.WarningAsync("Failed to send keepalive", ex).ConfigureAwait(false);
                    }

                    await Task.Delay(intervalMillis, cancelToken).ConfigureAwait(false);
                }
                await _audioLogger.DebugAsync("Keepalive Stopped").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await _audioLogger.DebugAsync("Keepalive Stopped").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _audioLogger.ErrorAsync("Keepalive Errored", ex).ConfigureAwait(false);
            }
        }

        public async Task SetSpeakingAsync(bool value)
        {
            if (_isSpeaking != value)
            {
                _isSpeaking = value;
                await ApiClient.SendSetSpeaking(value).ConfigureAwait(false);
            }
        }

        internal void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAsync().GetAwaiter().GetResult();
                ApiClient.Dispose();
                _stateLock?.Dispose();
            }
        }
        /// <inheritdoc />
        public void Dispose() => Dispose(true);
    }
}
