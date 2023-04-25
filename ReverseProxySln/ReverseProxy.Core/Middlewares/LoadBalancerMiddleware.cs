using Microsoft.AspNetCore.Http;
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ReverseProxy.Core.Middlewares
{
    public class LoadBalancerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IReadOnlyList<UriWithHash> _serverUris;
        private readonly ILoadBalancerStrategy _loadBalancerStrategy;

        private readonly string _sessionCookieName = "ReverseProxy_StickySession";
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
            //var serverUri = _loadBalancerStrategy.GetNextServerUri();
            Uri serverUri = null;

            // In Azure: ARRAffinity=d8ee64705542e2375ca834082505a2e598ec41611f45c9bed1a774ccbbc582a9; ARRAffinitySameSite=d8ee64705542e2375ca834082505a2e598ec41611f45c9bed1a774ccbbc582a9
            if (context.Request.Cookies.TryGetValue(_sessionCookieName, out string sessionId))
            {
                serverUri = _serverUris.FirstOrDefault(f => f.Hash.Equals(sessionId, StringComparison.OrdinalIgnoreCase))?.Uri;
            }
            if (serverUri == null)
            {
                var uriWithHash = _loadBalancerStrategy.GetNextServerUri();
                serverUri = uriWithHash.Uri;
                context.Response.Cookies.Append(_sessionCookieName, uriWithHash.Hash, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
            }

            var requestUri = new Uri(serverUri, context.Request.Path);
            var httpClient = _clientFactory.CreateClient(nameof(LoadBalancerMiddleware));

            Console.WriteLine($"Called {requestUri}");

            // Create a new HttpRequestMessage and copy the properties from the incoming request
            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = requestUri,
                Content = await GetContent(context.Request)
            };

            // Copy headers from the incoming request to the HttpRequestMessage
            //foreach (var header in context.Request.Headers)
            //{
            //    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Upgrade-Insecure-Requests
            //    if (header.Key.Equals("Upgrade-Insecure-Requests", StringComparison.OrdinalIgnoreCase))
            //    {
            //        continue;
            //    }
            //    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            //}

            // Send the request
            var responseMessage = await httpClient.SendAsync(requestMessage);

            context.Response.StatusCode = (int)responseMessage.StatusCode;
            var responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(responseContent, 0, responseContent.Length);
        }

        private static async Task<HttpContent> GetContent(HttpRequest request)
        {
            if (request.Body == null || (request.Method != HttpMethods.Post && request.Method != HttpMethods.Put && request.Method != HttpMethods.Delete))
            {
                return null;
            }

            var content = new MemoryStream();
            await request.Body.CopyToAsync(content);
            content.Seek(0, SeekOrigin.Begin);

            var contentType = request.ContentType;
            return new StreamContent(content) { Headers = { ContentType = MediaTypeHeaderValue.Parse(contentType) } };
        }
    }
}
