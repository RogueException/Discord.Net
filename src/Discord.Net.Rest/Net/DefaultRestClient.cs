using Discord.Net.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Net.Rest
{
    internal sealed class DefaultRestClient : IRestClient, IDisposable
    {
        private const int HR_SECURECHANNELFAILED = -2146233079;

        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly JsonSerializer _errorDeserializer;
        private CancellationToken _cancelToken;
        private bool _isDisposed;

        public DefaultRestClient(string baseUrl, bool useProxy = false)
        {
            _baseUrl = baseUrl;

#pragma warning disable IDISP014
            _client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false,
                UseProxy = useProxy,
            });
#pragma warning restore IDISP014
            SetHeader("accept-encoding", "gzip, deflate");

            _cancelToken = CancellationToken.None;
            _errorDeserializer = new JsonSerializer();
        }
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _client.Dispose();
                _isDisposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        public void SetHeader(string key, string value)
        {
            _client.DefaultRequestHeaders.Remove(key);
            if (value != null)
                _client.DefaultRequestHeaders.Add(key, value);
        }
        public void SetCancelToken(CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
        }

        public async Task<RestResponse> SendAsync(string method, string endpoint, CancellationToken cancelToken, bool headerOnly, string reason = null)
        {
            string uri = Path.Combine(_baseUrl, endpoint);
            using (var restRequest = new HttpRequestMessage(GetMethod(method), uri))
            {
                if (reason != null) restRequest.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
                return await SendInternalAsync(restRequest, cancelToken, headerOnly).ConfigureAwait(false);
            }
        }
        public async Task<RestResponse> SendAsync(string method, string endpoint, string json, CancellationToken cancelToken, bool headerOnly, string reason = null)
        {
            string uri = Path.Combine(_baseUrl, endpoint);
            using (var restRequest = new HttpRequestMessage(GetMethod(method), uri))
            {
                if (reason != null) restRequest.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
                restRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return await SendInternalAsync(restRequest, cancelToken, headerOnly).ConfigureAwait(false);
            }
        }
        public async Task<RestResponse> SendAsync(string method, string endpoint, IEnumerable<KeyValuePair<string?, string?>> formDataContent, CancellationToken cancelToken, bool headerOnly, string reason = null)
        {
            string uri = Path.Combine(_baseUrl, endpoint);
            using (var restRequest = new HttpRequestMessage(GetMethod(method), uri))
            {
                if (reason != null)
                    restRequest.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
                restRequest.Content = new FormUrlEncodedContent(formDataContent); // json, Encoding.UTF8, "application/json");
                return await SendInternalAsync(restRequest, cancelToken, headerOnly).ConfigureAwait(false);
            }
        }

        /// <exception cref="InvalidOperationException">Unsupported param type.</exception>
        public async Task<RestResponse> SendAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams, CancellationToken cancelToken, bool headerOnly, string reason = null)
        {
            string uri = Path.Combine(_baseUrl, endpoint);
            using (var restRequest = new HttpRequestMessage(GetMethod(method), uri))
            {
                if (reason != null) restRequest.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
                var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                MemoryStream memoryStream = null;
                if (multipartParams != null)
                {
                    foreach (var p in multipartParams)
                    {
                        switch (p.Value)
                        {
#pragma warning disable IDISP004
                            case string stringValue: { content.Add(new StringContent(stringValue), p.Key); continue; }
                            case byte[] byteArrayValue: { content.Add(new ByteArrayContent(byteArrayValue), p.Key); continue; }
                            case Stream streamValue: { content.Add(new StreamContent(streamValue), p.Key); continue; }
                            case MultipartFile fileValue:
                            {
                                var stream = fileValue.Stream;
                                if (!stream.CanSeek)
                                {
                                    memoryStream = new MemoryStream();
                                    await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                                    memoryStream.Position = 0;
#pragma warning disable IDISP001
                                    stream = memoryStream;
#pragma warning restore IDISP001
                                }
                                content.Add(new StreamContent(stream), p.Key, fileValue.Filename);
#pragma warning restore IDISP004
                                continue;
                            }
                            default: throw new InvalidOperationException($"Unsupported param type \"{p.Value.GetType().Name}\".");
                        }
                    }
                }
                restRequest.Content = content;
                var result = await SendInternalAsync(restRequest, cancelToken, headerOnly).ConfigureAwait(false);
                memoryStream?.Dispose();
                return result;
            }
        }

        private async Task<RestResponse> SendInternalAsync(HttpRequestMessage request, CancellationToken cancelToken, bool headerOnly)
        {
            using (var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancelToken, cancelToken))
            {
                cancelToken = cancelTokenSource.Token;
                HttpResponseMessage response = await _client.SendAsync(request, cancelToken).ConfigureAwait(false);

                var headers = response.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
                var stream = !headerOnly ? await response.Content.ReadAsStreamAsync().ConfigureAwait(false) : null;

                return new RestResponse(response.StatusCode, headers, stream);
            }
        }

        private static readonly HttpMethod Patch = new HttpMethod("PATCH");
        private HttpMethod GetMethod(string method)
        {
            switch (method)
            {
                case "DELETE": return HttpMethod.Delete;
                case "GET": return HttpMethod.Get;
                case "PATCH": return Patch;
                case "POST": return HttpMethod.Post;
                case "PUT": return HttpMethod.Put;
                default: throw new ArgumentOutOfRangeException(nameof(method), $"Unknown HttpMethod: {method}");
            }
        }
    }
}
