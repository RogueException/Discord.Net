using Discord.Net.Rest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Net.Queue
{
    public class FormRestRequest : RestRequest
    {
        public IEnumerable<KeyValuePair<string?, string?>> FormData { get; }

        public FormRestRequest(IRestClient client, string method, string endpoint, IEnumerable<KeyValuePair<string?, string?>> formData, RequestOptions options)
            : base(client, method, endpoint, options)
        {
            FormData = formData;
        }

        public override async Task<RestResponse> SendAsync()
        {
            return await Client.SendAsync(Method, Endpoint, FormData, Options.CancelToken, Options.HeaderOnly, Options.AuditLogReason).ConfigureAwait(false);
        }
    }
}
