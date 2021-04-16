using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
#if DEBUG_LIMITS
using System.Diagnostics;
#endif
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Net.Queue
{
    internal class RequestBucket
    {
        private const int MinimumSleepTimeMs = 750;

        private readonly object _lock;
        private readonly RequestQueue _queue;
        private int _semaphore;
        private DateTimeOffset? _resetTick;
        private RequestBucket _redirectBucket;

        public BucketId Id { get; private set; }
        public int WindowCount { get; private set; }
        public DateTimeOffset LastAttemptAt { get; private set; }

        public RequestBucket(RequestQueue queue, IRequest request, BucketId id)
        {
            _queue = queue;
            Id = id;

            _lock = new object();

            if (request.Options.IsClientBucket)
                WindowCount = ClientBucket.Get(request.Options.BucketId).WindowCount;
            else if (request.Options.IsGatewayBucket)
                WindowCount = GatewayBucket.Get(request.Options.BucketId).WindowCount;
            else
                WindowCount = 1; //Only allow one request until we get a header back
            _semaphore = WindowCount;
            _resetTick = null;
            LastAttemptAt = DateTimeOffset.UtcNow;
        }

        static int nextId = 0;
        public async Task<Stream> SendAsync(RestRequest request)
        {
            int id = Interlocked.Increment(ref nextId);
#if DEBUG_LIMITS
            Debug.WriteLine($"[{id}] Start");
#endif
            LastAttemptAt = DateTimeOffset.UtcNow;
            while (true)
            {
                await _queue.EnterGlobalAsync(id, request).ConfigureAwait(false);
                await EnterAsync(id, request).ConfigureAwait(false);
                if (_redirectBucket != null)
                    return await _redirectBucket.SendAsync(request);

#if DEBUG_LIMITS
                Debug.WriteLine($"[{id}] Sending...");
#endif
                RateLimitInfo info = default(RateLimitInfo);
                try
                {
                    var response = await request.SendAsync().ConfigureAwait(false);
                    info = new RateLimitInfo(response.Headers);

                    if (response.StatusCode < (HttpStatusCode)200 || response.StatusCode >= (HttpStatusCode)300)
                    {
                        switch (response.StatusCode)
                        {
                            case (HttpStatusCode)429:
                                if (info.IsGlobal)
                                {
#if DEBUG_LIMITS
                                    Debug.WriteLine($"[{id}] (!) 429 [Global]");
#endif
                                    _queue.PauseGlobal(info);
                                }
                                else
                                {
#if DEBUG_LIMITS
                                    Debug.WriteLine($"[{id}] (!) 429");
#endif
                                    UpdateRateLimit(id, request, info, true);
                                }
                                await _queue.RaiseRateLimitTriggered(Id, info, $"{request.Method} {request.Endpoint}").ConfigureAwait(false);
                                continue; //Retry
                            case HttpStatusCode.BadGateway: //502
#if DEBUG_LIMITS
                                Debug.WriteLine($"[{id}] (!) 502");
#endif
                                if ((request.Options.RetryMode & RetryMode.Retry502) == 0)
                                    throw new HttpException(HttpStatusCode.BadGateway, request, null);

                                continue; //Retry
                            default:
                                int? code = null;
                                string reason = null;
                                object errors = null;
                                if (response.Stream != null)
                                {
                                    try
                                    {
                                        using (var reader = new StreamReader(response.Stream))
                                        using (var jsonReader = new JsonTextReader(reader))
                                        {
                                            var json = JToken.Load(jsonReader);
                                            try { code = json.Value<int>("code"); } catch { };
                                            try { reason = json.Value<string>("message"); } catch { };
                                            try { errors = json.Value<object>("errors"); } catch { };
                                        }
                                    }
                                    catch { }
                                }
                                throw new HttpException(response.StatusCode, request, code, reason, errors);
                        }
                    }
                    else
                    {
#if DEBUG_LIMITS
                        Debug.WriteLine($"[{id}] Success");
#endif
                        return response.Stream;
                    }
                }
                //catch (HttpException) { throw; } //Pass through
                catch (TimeoutException)
                {
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Timeout");
#endif
                    if ((request.Options.RetryMode & RetryMode.RetryTimeouts) == 0)
                        throw;

                    await Task.Delay(500).ConfigureAwait(false);
                    continue; //Retry
                }
                /*catch (Exception)
                {
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Error");
#endif
                    if ((request.Options.RetryMode & RetryMode.RetryErrors) == 0)
                        throw;

                    await Task.Delay(500);
                    continue; //Retry
                }*/
                finally
                {
                    UpdateRateLimit(id, request, info, false);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Stop");
#endif
                }
            }
        }
        public async Task SendAsync(WebSocketRequest request)
        {
            int id = Interlocked.Increment(ref nextId);
#if DEBUG_LIMITS
            Debug.WriteLine($"[{id}] Start");
#endif
            LastAttemptAt = DateTimeOffset.UtcNow;
            while (true)
            {
                await _queue.EnterGlobalAsync(id, request).ConfigureAwait(false);
                await EnterAsync(id, request).ConfigureAwait(false);

#if DEBUG_LIMITS
                Debug.WriteLine($"[{id}] Sending...");
#endif
                try
                {
                    await request.SendAsync().ConfigureAwait(false);
                    return;
                }
                catch (TimeoutException)
                {
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Timeout");
#endif
                    if ((request.Options.RetryMode & RetryMode.RetryTimeouts) == 0)
                        throw;

                    await Task.Delay(500).ConfigureAwait(false);
                    continue; //Retry
                }
                /*catch (Exception)
                {
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Error");
#endif
                    if ((request.Options.RetryMode & RetryMode.RetryErrors) == 0)
                        throw;

                    await Task.Delay(500);
                    continue; //Retry
                }*/
                finally
                {
                    UpdateRateLimit(id, request, default(RateLimitInfo), false);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Stop");
#endif
                }
            }
        }

        internal async Task TriggerAsync(int id, IRequest request)
        {
#if DEBUG_LIMITS
            Debug.WriteLine($"[{id}] Trigger Bucket");
#endif
            await EnterAsync(id, request).ConfigureAwait(false);
            UpdateRateLimit(id, request, default(RateLimitInfo), false);
        }

        private async Task EnterAsync(int id, IRequest request)
        {
            int windowCount;
            DateTimeOffset? resetAt;
            bool isRateLimited = false;

            while (true)
            {
                if (_redirectBucket != null)
                    break;

                if (DateTimeOffset.UtcNow > request.TimeoutAt || request.Options.CancelToken.IsCancellationRequested)
                {
                    if (!isRateLimited)
                        throw new TimeoutException();
                    else
                        ThrowRetryLimit(request);
                }

                lock (_lock)
                {
                    windowCount = WindowCount;
                    resetAt = _resetTick;
                }

                DateTimeOffset? timeoutAt = request.TimeoutAt;
                int semaphore = Interlocked.Decrement(ref _semaphore);
                if (windowCount > 0 && semaphore < 0)
                {
                    if (!isRateLimited)
                    {
                        bool ignoreRatelimit = false;
                        isRateLimited = true;
                        switch (request)
                        {
                            case RestRequest restRequest:
                                await _queue.RaiseRateLimitTriggered(Id, null, $"{restRequest.Method} {restRequest.Endpoint}").ConfigureAwait(false);
                                break;
                            case WebSocketRequest webSocketRequest:
                                if (webSocketRequest.IgnoreLimit)
                                {
                                    ignoreRatelimit = true;
                                    break;
                                }
                                await _queue.RaiseRateLimitTriggered(Id, null, Id.Endpoint).ConfigureAwait(false);
                                break;
                            default:
                                throw new InvalidOperationException("Unknown request type");
                        }
                        if (ignoreRatelimit)
                        {
#if DEBUG_LIMITS
                            Debug.WriteLine($"[{id}] Ignoring ratelimit");
#endif
                            break;
                        }
                    }

                    ThrowRetryLimit(request);

                    if (resetAt.HasValue && resetAt > DateTimeOffset.UtcNow)
                    {
                        if (resetAt > timeoutAt)
                            ThrowRetryLimit(request);

                        int millis = (int)Math.Ceiling((resetAt.Value - DateTimeOffset.UtcNow).TotalMilliseconds);
#if DEBUG_LIMITS
                        Debug.WriteLine($"[{id}] Sleeping {millis} ms (Pre-emptive)");
#endif
                        if (millis > 0)
                            await Task.Delay(millis, request.Options.CancelToken).ConfigureAwait(false);
                    }
                    else
                    {
                        if ((timeoutAt.Value - DateTimeOffset.UtcNow).TotalMilliseconds < MinimumSleepTimeMs)
                            ThrowRetryLimit(request);
#if DEBUG_LIMITS
                        Debug.WriteLine($"[{id}] Sleeping {MinimumSleepTimeMs}* ms (Pre-emptive)");
#endif
                        await Task.Delay(MinimumSleepTimeMs, request.Options.CancelToken).ConfigureAwait(false);
                    }
                    continue;
                }
#if DEBUG_LIMITS
                else
                    Debug.WriteLine($"[{id}] Entered Semaphore ({semaphore}/{WindowCount} remaining)");
#endif
                break;
            }
        }

        private void UpdateRateLimit(int id, IRequest request, RateLimitInfo info, bool is429, bool redirected = false)
        {
            if (WindowCount == 0)
                return;

            lock (_lock)
            {
                if (redirected)
                {
                    Interlocked.Decrement(ref _semaphore); //we might still hit a real ratelimit if all tickets were already taken, can't do much about it since we didn't know they were the same
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Decrease Semaphore");
#endif
                }
                bool hasQueuedReset = _resetTick != null;

                if (info.Bucket != null && !redirected)
                {
                    (RequestBucket, BucketId) hashBucket = _queue.UpdateBucketHash(Id, info.Bucket);
                    if (!(hashBucket.Item1 is null) && !(hashBucket.Item2 is null))
                    {
                        if (hashBucket.Item1 == this) //this bucket got promoted to a hash queue
                        {
                            Id = hashBucket.Item2;
#if DEBUG_LIMITS
                            Debug.WriteLine($"[{id}] Promoted to Hash Bucket ({hashBucket.Item2})");
#endif
                        }
                        else
                        {
                            _redirectBucket = hashBucket.Item1; //this request should be part of another bucket, this bucket will be disabled, redirect everything
                            _redirectBucket.UpdateRateLimit(id, request, info, is429, redirected: true); //update the hash bucket ratelimit
#if DEBUG_LIMITS
                            Debug.WriteLine($"[{id}] Redirected to {_redirectBucket.Id}");
#endif
                            return;
                        }
                    }
                }

                if (info.Limit.HasValue && WindowCount != info.Limit.Value)
                {
                    WindowCount = info.Limit.Value;
                    _semaphore = info.Remaining.Value;
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Upgraded Semaphore to {info.Remaining.Value}/{WindowCount}");
#endif
                }

                DateTimeOffset? resetTick = null;

                //Using X-RateLimit-Remaining causes a race condition
                /*if (info.Remaining.HasValue)
                {
                    Debug.WriteLine($"[{id}] X-RateLimit-Remaining: " + info.Remaining.Value);
                    _semaphore = info.Remaining.Value;
                }*/
                if (info.RetryAfter.HasValue)
                {
                    //RetryAfter is more accurate than Reset, where available
                    resetTick = DateTimeOffset.UtcNow.AddMilliseconds(info.RetryAfter.Value);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Retry-After: {info.RetryAfter.Value} ({info.RetryAfter.Value} ms)");
#endif
                }
                else if (info.ResetAfter.HasValue && (request.Options.UseSystemClock.HasValue ? !request.Options.UseSystemClock.Value : false))
                {
                    resetTick = DateTimeOffset.UtcNow.Add(info.ResetAfter.Value);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Reset-After: {info.ResetAfter.Value} ({info.ResetAfter?.TotalMilliseconds} ms)");
#endif
                }
                else if (info.Reset.HasValue)
                {
                    resetTick = info.Reset.Value.AddSeconds(info.Lag?.TotalSeconds ?? 1.0);

                    /* millisecond precision makes this unnecessary, retaining in case of regression
                    if (request.Options.IsReactionBucket)
                        resetTick = DateTimeOffset.Now.AddMilliseconds(250);
					*/

                    int diff = (int)(resetTick.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] X-RateLimit-Reset: {info.Reset.Value.ToUnixTimeSeconds()} ({diff} ms, {info.Lag?.TotalMilliseconds} ms lag)");
#endif
                }
                else if (request.Options.IsClientBucket && Id != null)
                {
                    resetTick = DateTimeOffset.UtcNow.AddSeconds(ClientBucket.Get(Id).WindowSeconds);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Client Bucket ({ClientBucket.Get(Id).WindowSeconds * 1000} ms)");
#endif
                }
                else if (request.Options.IsGatewayBucket && request.Options.BucketId != null)
                {
                    resetTick = DateTimeOffset.UtcNow.AddSeconds(GatewayBucket.Get(request.Options.BucketId).WindowSeconds);
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Gateway Bucket ({GatewayBucket.Get(request.Options.BucketId).WindowSeconds * 1000} ms)");
#endif
                    if (!hasQueuedReset)
                    {
                        _resetTick = resetTick;
                        LastAttemptAt = resetTick.Value;
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Reset in {(int)Math.Ceiling((resetTick - DateTimeOffset.UtcNow).Value.TotalMilliseconds)} ms");
#endif
                        var _ = QueueReset(id, (int)Math.Ceiling((_resetTick.Value - DateTimeOffset.UtcNow).TotalMilliseconds), request);
                    }
                    return;
                }

                if (resetTick == null)
                {
                    WindowCount = 0; //No rate limit info, disable limits on this bucket
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Disabled Semaphore");
#endif
                    return;
                }

                if (!hasQueuedReset || resetTick > _resetTick)
                {
                    _resetTick = resetTick;
                    LastAttemptAt = resetTick.Value; //Make sure we dont destroy this until after its been reset
#if DEBUG_LIMITS
                    Debug.WriteLine($"[{id}] Reset in {(int)Math.Ceiling((resetTick - DateTimeOffset.UtcNow).Value.TotalMilliseconds)} ms");
#endif

                    if (!hasQueuedReset)
                    {
                        var _ = QueueReset(id, (int)Math.Ceiling((_resetTick.Value - DateTimeOffset.UtcNow).TotalMilliseconds), request);
                    }
                }
            }
        }
        private async Task QueueReset(int id, int millis, IRequest request)
        {
            while (true)
            {
                if (millis > 0)
                    await Task.Delay(millis).ConfigureAwait(false);
                lock (_lock)
                {
                    millis = (int)Math.Ceiling((_resetTick.Value - DateTimeOffset.UtcNow).TotalMilliseconds);
                    if (millis <= 0) //Make sure we havent gotten a more accurate reset time
                    {
#if DEBUG_LIMITS
                        Debug.WriteLine($"[{id}] * Reset *");
#endif
                        _semaphore = WindowCount;
                        _resetTick = null;
                        return;
                    }
                }
            }
        }

        private void ThrowRetryLimit(IRequest request)
        {
            if ((request.Options.RetryMode & RetryMode.RetryRatelimit) == 0)
                throw new RateLimitedException(request);
        }
    }
}
