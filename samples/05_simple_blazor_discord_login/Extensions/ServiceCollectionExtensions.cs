using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Discord.Net.Rest;
using Discord.Rest;

namespace _05_simple_blazor_discord_login.Extensions
{
    /// <summary>
    /// Here you will find all the extensions to be able to add a Discord Client to an IServiceCollection 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the DiscordRestClient as a Scoped Service to be able to use through DI.
        /// </summary>
        /// <param name="services">This is the IServiceCollection where all the services are located.</param>
        /// <param name="useProxy">Set this to true to use proxies, default is false.</param>
        /// <returns></returns>
        public static IServiceCollection AddDiscordRestClient(this IServiceCollection services, string clientId = null, string clientSecret = null, bool useProxy = false)
        {
            services.AddHttpClient("HttpClientFactoryRestClientProvider")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                    UseCookies = false,
                    UseProxy = useProxy,
                });



            //services.AddScoped<HttpClientFactoryRestClientProvider>(provider => new HttpClientFactoryRestClientProvider(provider.GetRequiredService<IHttpClientFactory>()));
            services.AddSingleton<DiscordRestClient>(provider =>
            {
                var config = new DiscordRestConfig
                {
                    RestClientProvider = HttpClientFactoryRestClientProvider.Create(provider.GetRequiredService<IHttpClientFactory>().CreateClient("HttpClientFactoryRestClientProvider"), useProxy),
                    ClientId = clientId,
                    ClientSecret = clientSecret
                    
                };
                return new DiscordRestClient(config);
            });

            return services;
        }
    }
}
