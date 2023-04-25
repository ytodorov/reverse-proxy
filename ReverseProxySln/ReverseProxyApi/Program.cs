using ReverseProxy.Core.Middlewares;
using ReverseProxy.Core.Extensions;

namespace ReverseProxyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLoadBalancer();

            var app = builder.Build();

            app.UseMiddleware<LoadBalancerMiddleware>();

            app.Run();
        }
    }
}