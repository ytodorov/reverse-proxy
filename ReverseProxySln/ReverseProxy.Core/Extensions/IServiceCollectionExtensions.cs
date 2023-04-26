using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using ReverseProxy.Core.Middlewares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddLoadBalancer(this IServiceCollection services)
        {
            services.AddSingleton<IServerUriProvider, ConfigurationServerUriProvider>();
            services.AddSingleton<ILoadBalancerStrategy, RoundRobinLoadBalancerStrategy>();
            services.AddSingleton<IStickySession, StickySessionDisabled>();
            services.AddSingleton<IHealthCheck, ServerHealthCheck>();
            // Change this to the following line to enable sticky sessions
            //services.AddSingleton<IStickySession, StickySessionEnabled>();

            services.AddHttpClient(nameof(LoadBalancerMiddleware), client =>
            {
                // Set a timeout for requests
                // https://learn.microsoft.com/en-us/troubleshoot/azure/app-service/web-apps-performance-faqs#why-does-my-request-time-out-after-230-seconds
                client.Timeout = TimeSpan.FromSeconds(230);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                //AllowAutoRedirect = true,
                // Configure the HttpClientHandler, e.g., enable automatic decompression for better performance
                //AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,
            })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
            //.AddPolicyHandler(/* Add a Polly policy for retries, timeouts, or other policies if needed */);

            return services;
        }
    }
}
