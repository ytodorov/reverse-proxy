using ReverseProxy.Core.Middlewares;
using ReverseProxy.Core.Extensions;
using System.Threading.RateLimiting;
using System.Globalization;

namespace ReverseProxyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRateLimiter(options =>
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
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(10)
                        });
                });
            });

            builder.Services.AddLoadBalancer();

            var app = builder.Build();

            app.UseRateLimiter();
            app.UseMiddleware<LoadBalancerMiddleware>();

            app.Run();
        }
    }
}