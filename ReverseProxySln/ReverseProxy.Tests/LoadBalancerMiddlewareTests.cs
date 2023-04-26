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
using Microsoft.Net.Http.Headers;

namespace ReverseProxy.Tests
{
    public class LoadBalancerMiddlewareTests
    {
        [Fact]
        public async Task RoundRobin_Should_Be_Different()
        {
            // Arrange
            using var server = CreateTestServer();
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            var response2 = await client.GetAsync("/");
            var content2 = await response2.Content.ReadAsStringAsync();

            var response3 = await client.GetAsync("/");
            var content3 = await response3.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEqual(content, content2);
            Assert.NotEqual(content, content3);
            Assert.NotEqual(content2, content3);
        }

        [Fact]
        public async Task RoundRobin_WithStickySession_Should_Be_Equal()
        {
            // Arrange
            using var server = CreateTestServer(true);
            HttpClient clie = new HttpClient();

            var client = server.CreateClient();
            
            
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("/", UriKind.Relative)
            };
            
            // Act
            var response = await client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("/", UriKind.Relative)
            };

            var cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestMessage.Headers.Add(HeaderNames.Cookie, cookie);

            var response2 = await client.SendAsync(requestMessage);
            var content2 = await response2.Content.ReadAsStringAsync();

            requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("/", UriKind.Relative)
            };
            cookie = response2.Headers.GetValues(HeaderNames.SetCookie);
            requestMessage.Headers.Add(HeaderNames.Cookie, cookie);

            var response3 = await client.SendAsync(requestMessage);
            var content3 = await response3.Content.ReadAsStringAsync();
            

            // Assert
            Assert.Equal(content, content2);
            Assert.Equal(content, content3);
            Assert.Equal(content2, content3);
        }

        private TestServer CreateTestServer(bool useStikySession = false)
        {
            var webHostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration(s => s.AddJsonFile("appsettings.json"))                
                .ConfigureServices(services =>
                {
                    services.AddLoadBalancer();
                    if (useStikySession)
                    {
                        services.AddSingleton<IStickySession, StickySessionEnabled>();
                    }
                })
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