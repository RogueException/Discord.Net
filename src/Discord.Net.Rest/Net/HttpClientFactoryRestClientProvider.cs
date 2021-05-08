using System;
using System.Net.Http;

namespace Discord.Net.Rest
{
    public static class HttpClientFactoryRestClientProvider
    {

        /// <exception cref="PlatformNotSupportedException">The default RestClientProvider is not supported on this platform.</exception>
        public static RestClientProvider Create(HttpClient httpClient, bool useProxy = false)
        {
            return url =>
            {
                try
                {
                    return new HttpClientFactoryRestClient(url, httpClient, useProxy);
                }
                catch (PlatformNotSupportedException ex)
                {
                    throw new PlatformNotSupportedException("The default RestClientProvider is not supported on this platform.", ex);
                }
            };
        }
    }
}
