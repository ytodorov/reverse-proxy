
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using ReverseProxy.Core.Middlewares;
using System.Net.Http.Headers;

namespace ReverseProxyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<IServerUriProvider, ConfigurationServerUriProvider>();
            builder.Services.AddHttpClient(nameof(LoadBalancerMiddleware), client =>
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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseMiddleware<LoadBalancerMiddleware>(); // Register the Load Balancer Middleware

            app.MapControllers();

            app.Run();
        }
    }
}