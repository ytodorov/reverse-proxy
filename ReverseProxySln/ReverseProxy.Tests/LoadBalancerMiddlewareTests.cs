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
using SampleReverseProxy.Core.Classes;
using System.Text.Json;
using System.Net.Http.Headers;

namespace ReverseProxy.Tests
{
    public class LoadBalancerMiddlewareTests
    {
        [Fact]
        public async Task ConcurrencyTest()
        {
            // Arrange
            using var server = CreateTestServer();
            var client = server.CreateClient();

            // Act

            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 99; i++)
            {
                tasks.Add(client.GetAsync("/"));
            }

            await Task.WhenAll(tasks);

            List<HttpStatusCode> statusCodeResults = new List<HttpStatusCode>();
            List<string> responses = new List<string>();

            for (int i = 0; i < 99; i++)
            {
                statusCodeResults.Add(tasks[i].Result.StatusCode);
                responses.Add(await tasks[i].Result.Content.ReadAsStringAsync());
            }

            Assert.True(statusCodeResults.All(r => r == HttpStatusCode.OK));
            Assert.True(responses.All(r => r.StartsWith("This request is received on", StringComparison.OrdinalIgnoreCase)));

            //var groups = responses.GroupBy(r => r);
            //Assert.Equal(3, groups.Count());

            //foreach (var gr in groups)
            //{
            //    Assert.Equal(33, gr.Count());
            //}
        }


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

        [Fact]
        public async Task Post_Should_Succeed()
        {
            // Arrange
            using var server = CreateTestServer(true);
            var client = server.CreateClient();

            var guidForProductName = Guid.NewGuid().ToString();
            var productModel = new ProductViewModel()
            {
                Id = Random.Shared.Next(0, int.MaxValue),
                Name = guidForProductName,
                Price = (decimal)Random.Shared.NextDouble(),
            };
            StringContent sc = new StringContent(JsonSerializer.Serialize(productModel), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/product", sc);
            var content = await response.Content.ReadAsStringAsync();

            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("/product", UriKind.Relative)
            };

            var cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestMessage.Headers.Add(HeaderNames.Cookie, cookie);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var productsResponse = await client.SendAsync(requestMessage);
            var json = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductViewModel>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            var newProduct = products?.FirstOrDefault(f => f.Name == guidForProductName);

            Assert.NotNull(newProduct);

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