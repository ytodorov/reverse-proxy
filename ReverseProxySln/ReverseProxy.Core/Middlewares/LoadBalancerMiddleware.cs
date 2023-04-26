using Microsoft.AspNetCore.Http;
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ReverseProxy.Core.Middlewares
{
    public class LoadBalancerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDictionary<string, Uri> _serverUris;
        private readonly ILoadBalancerStrategy _loadBalancerStrategy;
        private readonly IStickySession _stickySession;

        private readonly string _sessionCookieName = "ReverseProxy_StickySession";
        public LoadBalancerMiddleware(RequestDelegate next,
            IHttpClientFactory clientFactory,
            IServerUriProvider serverUriProvider,
            ILoadBalancerStrategy loadBalancerStrategy,
            IStickySession stickySession
            )
        {
            _next = next;
            _clientFactory = clientFactory;
            _serverUris = serverUriProvider.GetServerUris();
            _loadBalancerStrategy = loadBalancerStrategy;
            _stickySession = stickySession;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                Uri serverUri = _loadBalancerStrategy.GetNextServerUri();

                if (_stickySession.IsStickySessionEnabled())
                {
                    // In Azure: ARRAffinity=d8ee64705542e2375ca834082505a2e598ec41611f45c9bed1a774ccbbc582a9; ARRAffinitySameSite=d8ee64705542e2375ca834082505a2e598ec41611f45c9bed1a774ccbbc582a9
                    if (context.Request.Cookies.TryGetValue(_sessionCookieName, out string hashedServerUri))
                    {
                        if (_serverUris.ContainsKey(hashedServerUri))
                        {
                            serverUri = _serverUris[hashedServerUri];
                        }
                    }
                    var hash = _serverUris.FirstOrDefault(f => f.Value == serverUri).Key;
                    context.Response.Cookies.Append(_sessionCookieName, hash, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
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
                // This could thow an exception: SocketException: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.
                var responseMessage = await httpClient.SendAsync(requestMessage);

                context.Response.StatusCode = (int)responseMessage.StatusCode;
                var responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
                await context.Response.Body.WriteAsync(responseContent, 0, responseContent.Length);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var errorContent = System.Text.Encoding.UTF8.GetBytes(ex.Message);
                await context.Response.Body.WriteAsync(errorContent, 0, errorContent.Length);
            }
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
