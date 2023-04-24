using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace ReverseProxy.Core.Middlewares
{
    public class LoadBalancerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _clientFactory;

        private static List<Uri> _serverUris;
        private static int _currentServerIndex = 0;

        public LoadBalancerMiddleware(RequestDelegate next, IHttpClientFactory clientFactory)
        {
            _next = next;
            _clientFactory = clientFactory;

            _serverUris = new List<Uri>
            {
                new Uri("https://www.microsoft.com/"),
                new Uri("https://www.apple.com/"),
                new Uri("https://www.google.com/")
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var serverUri = GetNextServerUri();
            var requestUri = new Uri(serverUri, context.Request.Path);
            var httpClient = _clientFactory.CreateClient(nameof(LoadBalancerMiddleware));

            Console.WriteLine($"Called {requestUri}");

            var responseMessage = await httpClient.GetAsync(requestUri);

            context.Response.StatusCode = (int)responseMessage.StatusCode;
            var responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(responseContent, 0, responseContent.Length);
        }

        private static Uri GetNextServerUri()
        {
            var serverUri = _serverUris[_currentServerIndex];
            _currentServerIndex = (_currentServerIndex + 1) % _serverUris.Count;
            
            return serverUri;
        }
    }
}
