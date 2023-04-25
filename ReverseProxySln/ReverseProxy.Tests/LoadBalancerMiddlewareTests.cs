using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using ReverseProxy.Core.Classes;
using ReverseProxy.Core.Interfaces;
using ReverseProxy.Core.Middlewares;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using ReverseProxy.Core.Extensions;

namespace ReverseProxy.Tests
{
    public class LoadBalancerMiddlewareTests
    {
        [Fact]
        public async Task RoundRobin_Should_Be_Different()
        {
            using var server = CreateTestServer();
            var client = server.CreateClient();

            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            var response2 = await client.GetAsync("/");
            var content2 = await response2.Content.ReadAsStringAsync();

            var response3 = await client.GetAsync("/");
            var content3 = await response3.Content.ReadAsStringAsync();

            Assert.NotEqual(content, content2);
            Assert.NotEqual(content, content3);
            Assert.NotEqual(content2, content3);


        }

        private TestServer CreateTestServer()
        {
            var webHostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration(s => s.AddJsonFile("appsettings.json"))
                .ConfigureServices(services => services.AddLoadBalancer())
                .Configure(app =>
                {
                    app.UseMiddleware<LoadBalancerMiddleware>();
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Test response");
                    });
                });

            return new TestServer(webHostBuilder);
        }
    }
}