using Microsoft.AspNetCore.Http;
using ReverseProxy.Core.Interfaces;
using System.Diagnostics.Metrics;
using System.Net.Http;

namespace ReverseProxy.Core.Middlewares
{
    public class LoadBalancerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IReadOnlyList<Uri> _serverUris;
        private readonly ILoadBalancerStrategy _loadBalancerStrategy;

        public LoadBalancerMiddleware(RequestDelegate next,
            IHttpClientFactory clientFactory,
            IServerUriProvider serverUriProvider,
            ILoadBalancerStrategy loadBalancerStrategy
            )
        {
            _next = next;
            _clientFactory = clientFactory;
            _serverUris = serverUriProvider.GetServerUris();
            _loadBalancerStrategy = loadBalancerStrategy;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var serverUri = _loadBalancerStrategy.GetNextServerUri();
            var requestUri = new Uri(serverUri, context.Request.Path);
            var httpClient = _clientFactory.CreateClient(nameof(LoadBalancerMiddleware));

            Console.WriteLine($"Called {requestUri}");

            var responseMessage = await httpClient.GetAsync(requestUri);

            context.Response.StatusCode = (int)responseMessage.StatusCode;
            var responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(responseContent, 0, responseContent.Length);
        }
    }
}
