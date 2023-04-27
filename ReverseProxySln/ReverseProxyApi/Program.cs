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
                        
            builder.Services.AddLoadBalancer();

            var app = builder.Build();

            app.UseLoadBalancerMiddleware();

            app.Run();
        }
    }
}