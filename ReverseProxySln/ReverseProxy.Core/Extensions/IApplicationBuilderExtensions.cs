using ReverseProxy.Core.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ReverseProxy.Core.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseLoadBalancerMiddleware(this IApplicationBuilder app)
        {
            app.UseRateLimiter();
            app.UseMiddleware<LoadBalancerMiddleware>();
            return app;
        }
    }
}
