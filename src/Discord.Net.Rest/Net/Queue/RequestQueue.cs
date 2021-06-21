using Newtonsoft.Json.Bson;
using System;
using System.Collections.Concurrent;
#if DEBUG_LIMITS
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Net.Queue
{
    internal class RequestQueue : IDisposable, IAsyncDisposable
    {
        public event Func<BucketId, RateLimitInfo?, string, Task> RateLimitTriggered;

        private readonly ConcurrentDictionary<BucketId, object> _buckets;
        private readonly SemaphoreSlim _tokenLock;
        private readonly CancellationTokenSource _cancelTokenSource; //Dispose token
        private CancellationTokenSource _clearToken;
        private CancellationToken _parentToken;
        private CancellationTokenSource _requestCancelTokenSource;
        private CancellationToken _requestCancelToken; //Parent token + Clear token
        private DateTimeOffset _waitUntil;

        private Task _cleanupTask;

        public RequestQueue()
        {
            _tokenLock = new SemaphoreSlim(1, 1);

            _clearToken = new CancellationTokenSource();
            _cancelTokenSource = new CancellationTokenSource();
            _requestCancelToken = CancellationToken.None;
            _parentToken = CancellationToken.None;

            _buckets = new ConcurrentDictionary<BucketId, object>();

            _cleanupTask = RunCleanup();
        }

        public async Task SetCancelTokenAsync(CancellationToken cancelToken)
        {
            await _tokenLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _parentToken = cancelToken;
                _requestCancelTokenSource?.Dispose();
                _requestCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _clearToken.Token);
                _requestCancelToken = _requestCancelTokenSource.Token;
            }
            finally { _tokenLock.Release(); }
        }
        public async Task ClearAsync()
        {
            await _tokenLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _clearToken?.Cancel();
                _clearToken?.Dispose();
                _clearToken = new CancellationTokenSource();
                if (_parentToken != null)
                {
                    _requestCancelTokenSource?.Dispose();
                    _requestCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_clearToken.Token, _parentToken);
                    _requestCancelToken = _requestCancelTokenSource.Token;
                }
                else
                    _requestCancelToken = _clearToken.Token;
            }
            finally { _tokenLock.Release(); }
        }

        public async Task<Stream> SendAsync(RestRequest request)
        {
            CancellationTokenSource createdTokenSource = null;
            if (request.Options.CancelToken.CanBeCanceled)
            {
                createdTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_requestCancelToken, request.Options.CancelToken);
                request.Options.CancelToken = createdTokenSource.Token;
            }
            else
                request.Options.CancelToken = _requestCancelToken;

            var bucket = GetOrCreateBucket(request.Options, request);
            var result = await bucket.SendAsync(request).ConfigureAwait(false);
            createdTokenSource?.Dispose();
            return result;
        }
        public async Task SendAsync(WebSocketRequest request)
        {
            CancellationTokenSource createdTokenSource = null;
            if (request.Options.CancelToken.CanBeCanceled)
            {
                createdTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_requestCancelToken, request.Options.CancelToken);
                request.Options.CancelToken = createdTokenSource.Token;
            }
            else
                request.Options.CancelToken = _requestCancelToken;

            var bucket = GetOrCreateBucket(request.Options, request);
            await bucket.SendAsync(request).ConfigureAwait(false);
            createdTokenSource?.Dispose();
        }

        internal async Task EnterGlobalAsync(int id, RestRequest request)
        {
            int millis = (int)Math.Ceiling((_waitUntil - DateTimeOffset.UtcNow).TotalMilliseconds);
            if (millis > 0)
            {
#if DEBUG_LIMITS
                Debug.WriteLine($"[{id}] Sleeping {millis} ms (Pre-emptive) [Global]");
#endif
                await Task.Delay(millis).ConfigureAwait(false);
            }
        }
        internal void PauseGlobal(RateLimitInfo info)
        {
            _waitUntil = DateTimeOffset.UtcNow.AddMilliseconds(info.RetryAfter.Value + (info.Lag?.TotalMilliseconds ?? 0.0));
        }
        internal async Task EnterGlobalAsync(int id, WebSocketRequest request)
        {
            //If this is a global request (unbucketed), it'll be dealt in EnterAsync
            var requestBucket = GatewayBucket.Get(request.Options.BucketId);
            if (requestBucket.Type == GatewayBucketType.Unbucketed)
                return;

            //It's not a global request, so need to remove one from global (per-session)
            var globalBucketType = GatewayBucket.Get(GatewayBucketType.Unbucketed);
            var options = RequestOptions.CreateOrClone(request.Options);
            options.BucketId = globalBucketType.Id;
            var globalRequest = new WebSocketRequest(null, null, false, false, options);
            var globalBucket = GetOrCreateBucket(options, globalRequest);
            await globalBucket.TriggerAsync(id, globalRequest);
        }

        private RequestBucket GetOrCreateBucket(RequestOptions options, IRequest request)
        {
            var bucketId = options.BucketId;
            object obj = _buckets.GetOrAdd(bucketId, x => new RequestBucket(this, request, x));
            if (obj is BucketId hashBucket)
            {
                options.BucketId = hashBucket;
                return (RequestBucket)_buckets.GetOrAdd(hashBucket, x => new RequestBucket(this, request, x));
            }
            return (RequestBucket)obj;
        }
        internal async Task RaiseRateLimitTriggered(BucketId bucketId, RateLimitInfo? info, string endpoint)
        {
            await RateLimitTriggered(bucketId, info, endpoint).ConfigureAwait(false);
        }
        internal (RequestBucket, BucketId) UpdateBucketHash(BucketId id, string discordHash)
        {
            if (!id.IsHashBucket)
            {
                var bucket = BucketId.Create(discordHash, id);
                var hashReqQueue = (RequestBucket)_buckets.GetOrAdd(bucket, _buckets[id]);
                _buckets.AddOrUpdate(id, bucket, (oldBucket, oldObj) => bucket);
                return (hashReqQueue, bucket);
            }
            return (null, null);
        }

        public void ClearGatewayBuckets()
        {
            foreach (var gwBucket in (GatewayBucketType[])Enum.GetValues(typeof(GatewayBucketType)))
                _buckets.TryRemove(GatewayBucket.Get(gwBucket).Id, out _);
        }

        private async Task RunCleanup()
        {
            try
            {
                while (!_cancelTokenSource.IsCancellationRequested)
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var bucket in _buckets.Where(x => x.Value is RequestBucket).Select(x => (RequestBucket)x.Value))
                    {
                        if ((now - bucket.LastAttemptAt).TotalMinutes > 1.0)
                        {
                            if (bucket.Id.IsHashBucket)
                                foreach (var redirectBucket in _buckets.Where(x => x.Value == bucket.Id).Select(x => (BucketId)x.Value))
                                    _buckets.TryRemove(redirectBucket, out _); //remove redirections if hash bucket
                            _buckets.TryRemove(bucket.Id, out _);
                        }
                    }
                    await Task.Delay(60000, _cancelTokenSource.Token).ConfigureAwait(false); //Runs each minute
                }
            }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            if (!(_cancelTokenSource is null))
            {
                _cancelTokenSource.Cancel();
                _cancelTokenSource.Dispose();
                _cleanupTask.GetAwaiter().GetResult();
            }
            _tokenLock?.Dispose();
            _clearToken?.Dispose();
            _requestCancelTokenSource?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (!(_cancelTokenSource is null))
            {
                _cancelTokenSource.Cancel();
                _cancelTokenSource.Dispose();
                await _cleanupTask.ConfigureAwait(false);
            }
            _tokenLock?.Dispose();
            _clearToken?.Dispose();
            _requestCancelTokenSource?.Dispose();
        }
    }
}
