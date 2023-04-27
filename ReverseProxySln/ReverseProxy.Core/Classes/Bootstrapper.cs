using ReverseProxy.Core.Extensions;
using ReverseProxy.Core.Middlewares;

namespace ReverseProxy.Core.Classes
{
    public static class Bootstrapper
    {
        public static void StartLoadBalancer(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLoadBalancer();

            var app = builder.Build();

            app.UseMiddleware<LoadBalancerMiddleware>();

            app.Run();
        }
    }
}
