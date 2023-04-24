using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Core.Middlewares;
using System.Net;
using System.Net.Http;

namespace ReverseProxy.Tests
{
    public class LoadBalancerMiddlewareTests
    {
        private readonly IHttpClientFactory _clientFactory;

        public LoadBalancerMiddlewareTests()
        {
            var services = new ServiceCollection();
            services.AddHttpClient(nameof(LoadBalancerMiddleware));
            var serviceProvider = services.BuildServiceProvider();

            _clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        }

        //[Fact]
        //public async Task Test_InvokeAsync_DelegatesRequest()
        //{
        //    var middleware = new LoadBalancerMiddleware(context => Task.CompletedTask, _clientFactory);
        //    var httpContext = new DefaultHttpContext();

        //    await middleware.InvokeAsync(httpContext);

        //    Assert.Equal(0, LoadBalancerMiddleware._currentServerIndex);
        //}

        //[Fact]
        //public async Task Test_InvokeAsync_LoadBalancesRequests()
        //{
        //    var middleware = new LoadBalancerMiddleware(context => Task.CompletedTask, _clientFactory);
        //    var httpContext = new DefaultHttpContext();

        //    await middleware.InvokeAsync(httpContext);
        //    await middleware.InvokeAsync(httpContext);
        //    await middleware.InvokeAsync(httpContext);

        //    Assert.Equal(0, LoadBalancerMiddleware._currentServerIndex);
        //}

        [Fact]
        public async Task Test_InvokeAsync_HandlesResponse()
        {
            var middleware = new LoadBalancerMiddleware(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Hello World");
            }, _clientFactory);
            var httpContext = new DefaultHttpContext();

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            Assert.Equal((int)HttpStatusCode.OK, httpContext.Response.StatusCode);
            Assert.Equal("Hello World", responseBody);
        }
    }
}