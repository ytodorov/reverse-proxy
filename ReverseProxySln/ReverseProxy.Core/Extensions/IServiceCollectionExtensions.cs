using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using ReverseProxy.Core.Middlewares;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddLoadBalancer(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.OnRejected = (context, ct) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");

                    return new ValueTask();
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.Request.Headers.Host.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            // These numbers here are just for demonstration
                            PermitLimit = 200,
                            Window = TimeSpan.FromSeconds(10)
                        });
                });
            });

            // Add Application Insights services
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.EnableAdaptiveSampling = false;
            });

            services.AddSingleton<IServerUriProvider, ConfigurationServerUriProvider>();
            services.AddSingleton<ILoadBalancerStrategy, RoundRobinLoadBalancerStrategy>();
            services.AddSingleton<IStickySession, ConfigurationStickySession>();
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
            //.AddPolicyHandler(/* Add a Polly policy for retries, timeouts, or other policies if needed */); Use Polly here

            return services;
        }
    }
}
