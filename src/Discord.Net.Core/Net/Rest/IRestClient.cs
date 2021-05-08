using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Net.Rest
{
    /// <summary>
    ///     Represents a generic REST-based client.
    /// </summary>
    public interface IRestClient : IDisposable
    {
        /// <summary>
        ///     Sets the HTTP header of this client for all requests.
        /// </summary>
        /// <param name="key">The field name of the header.</param>
        /// <param name="value">The value of the header.</param>
        void SetHeader(string key, string value);
        /// <summary>
        ///     Sets the cancellation token for this client.
        /// </summary>
        /// <param name="cancelToken">The cancellation token.</param>
        void SetCancelToken(CancellationToken cancelToken);

        /// <summary>
        /// Sends a REST request.
        /// </summary>
        /// <param name="method">The method used to send this request (i.e. HTTP verb such as <c>GET</c>, <c>POST</c>).</param>
        /// <param name="endpoint">The endpoint to send this request to.</param>
        /// <param name="cancelToken">The cancellation token used to cancel the task.</param>
        /// <param name="headerOnly">Indicates whether to send the header only.</param>
        /// <param name="reason">The audit log reason.</param>
        /// <returns></returns>
        Task<RestResponse> SendAsync(string method, string endpoint, CancellationToken cancelToken, bool headerOnly = false, string reason = null);
        Task<RestResponse> SendAsync(string method, string endpoint, string json, CancellationToken cancelToken, bool headerOnly = false, string reason = null);
        Task<RestResponse> SendAsync(string method, string endpoint, IEnumerable<KeyValuePair<string?, string?>> formDataContent, CancellationToken cancelToken, bool headerOnly, string reason = null);
        Task<RestResponse> SendAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams, CancellationToken cancelToken, bool headerOnly = false, string reason = null);
    }
}
