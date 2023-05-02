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
using System.Net.Sockets;
using System.IO.Compression;
using System.Diagnostics;
using CliWrap;

namespace ReverseProxy.Tests
{
    public class LoadBalancerMiddlewareTests : IDisposable
    {
        private List<int> processIds = new List<int>();
        private List<string> tempDirs = new List<string>();
        private List<string> urlList = new List<string>();

        public LoadBalancerMiddlewareTests()
        {
            int numberOfBackendApis = 3; // We can easily simulate big number
            // Extract the zip file to the target directory            
            for (int i = 1; i <= numberOfBackendApis; i++)
            {
                var dirName = Path.Combine(Environment.CurrentDirectory, $"Api{Guid.NewGuid()}");
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                else
                {
                    Directory.Delete(dirName, true);
                    Directory.CreateDirectory(dirName);
                }
                ZipFile.ExtractToDirectory("TestBackendApi.zip", dirName, true);

                var randomFreePort = GetAvailableTcpPort();
                urlList.Add($"http://localhost:{randomFreePort}");
                var sampleApiPath = Path.Combine(dirName, "SampleApi.exe");

                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                var command =  Cli.Wrap(sampleApiPath)
                    .WithArguments(randomFreePort.ToString())
                    .WithWorkingDirectory(dirName)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync();

                tempDirs.Add(dirName);
                processIds.Add(command.ProcessId);
                                
                //var stdOut = stdOutBuffer.ToString();
                //var stdErr = stdErrBuffer.ToString();
            }
        }

        public void Dispose()
        {
            foreach (var processId in processIds)
            {
                var processToKill = Process.GetProcessById(processId);
                processToKill.Kill();
            }

            // Wait a while OS to release the process folder
            Thread.Sleep(2000);

            foreach (var dirToDelete in tempDirs)
            {
                Directory.Delete(dirToDelete, true);
            }
        }


            [Fact]
        public async Task ConcurrencyTest()
        {
            // Arrange
            using var server = await CreateTestServer();
            var client = server.CreateClient();

            // Act
            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < byte.MaxValue; i++)
            {
                tasks.Add(client.GetAsync("/"));
            }

            await Task.WhenAll(tasks);

            List<HttpStatusCode> statusCodeResults = new List<HttpStatusCode>();
            List<string> responses = new List<string>();

            for (int i = 0; i < byte.MaxValue; i++)
            {
                statusCodeResults.Add(tasks[i].Result.StatusCode);
                responses.Add(await tasks[i].Result.Content.ReadAsStringAsync());
            }

            Assert.True(statusCodeResults.All(r => r == HttpStatusCode.OK));
            Assert.True(responses.All(r => r.StartsWith("This request is received on", StringComparison.OrdinalIgnoreCase)));            
        }


        [Fact]
        public async Task RoundRobin_Should_Be_Different()
        {         

            // Arrange
            using var server = await CreateTestServer();
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
            using var server = await CreateTestServer(true);
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
            using var server = await CreateTestServer(true);
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
            Assert.Equal(productModel.Name, newProduct.Name);
        }

        [Fact]
        public async Task Put_Should_Succeed()
        {
            // Arrange
            using var server = await CreateTestServer(true);
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

            var newProduct = JsonSerializer.Deserialize<ProductViewModel>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("/product", UriKind.Relative)
            };

            var cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestMessage.Headers.Add(HeaderNames.Cookie, cookie);
                                    
            var guidForUpdatedProductName = Guid.NewGuid().ToString();
            var productUpdateModel = new ProductViewModel()
            {
                Id = Random.Shared.Next(0, int.MaxValue),
                Name = Guid.NewGuid().ToString(),
                Price = (decimal)Random.Shared.NextDouble(),
            };
            StringContent scForUpdate = new StringContent(JsonSerializer.Serialize(productUpdateModel), Encoding.UTF8, "application/json");

            var requestUpdateMessage = new HttpRequestMessage
            {
                RequestUri = new Uri($"/product/{newProduct.Id}", UriKind.Relative),
                Method = HttpMethod.Put,
                Content = scForUpdate
            };

            cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestUpdateMessage.Headers.Add(HeaderNames.Cookie, cookie);

            // Update product
            await client.SendAsync(requestUpdateMessage);

            var requestGetAllProducts = new HttpRequestMessage
            {
                RequestUri = new Uri($"/product", UriKind.Relative),
            };

            cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestGetAllProducts.Headers.Add(HeaderNames.Cookie, cookie);

            var productsResponse = await client.SendAsync(requestGetAllProducts);
            var json = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductViewModel>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            var updatedProduct = products?.FirstOrDefault(f => f.Name == productUpdateModel.Name);

            Assert.NotNull(updatedProduct);
            Assert.Equal(updatedProduct.Name, productUpdateModel.Name);

        }

        [Fact]
        public async Task Delete_Should_Succeed()
        {
            // Arrange
            using var server = await CreateTestServer(true);
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

            var newProduct = JsonSerializer.Deserialize<ProductViewModel>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            
            var requestDeleteMessage = new HttpRequestMessage
            {
                RequestUri = new Uri($"/product/{newProduct.Id}", UriKind.Relative),
                Method = HttpMethod.Delete
            };

            var cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestDeleteMessage.Headers.Add(HeaderNames.Cookie, cookie);

            // Update product
            await client.SendAsync(requestDeleteMessage);

            var requestGetAllProducts = new HttpRequestMessage
            {
                RequestUri = new Uri($"/product", UriKind.Relative),
            };

            cookie = response.Headers.GetValues(HeaderNames.SetCookie);
            requestGetAllProducts.Headers.Add(HeaderNames.Cookie, cookie);

            var productsResponse = await client.SendAsync(requestGetAllProducts);
            var json = await productsResponse.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductViewModel>>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            var deletedProduct = products?.FirstOrDefault(f => f.Name == newProduct.Name);

            Assert.Null(deletedProduct);

        }

        private async Task<TestServer> CreateTestServer(bool useStikySession = false)
        {
            var urlsDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, Uri>();

            foreach (var url in urlList)
            {
                urlsDictionary.TryAdd(url.CalculateSHA256(), new Uri(url));
            }

            var mockServerUriProvider = new Mock<IServerUriProvider>();
            mockServerUriProvider.Setup(provider => provider.GetServerUris())
                .Returns(urlsDictionary);

            var webHostBuilder = new WebHostBuilder()
                .ConfigureAppConfiguration(s => s.AddJsonFile("appsettings.json"))
                .ConfigureServices(services =>
                {
                    services.AddLoadBalancer();
                    services.AddSingleton<IServerUriProvider>(mockServerUriProvider.Object);
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

        private static int GetAvailableTcpPort()
        {
            int availablePort;

            using (Socket tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                tempSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                availablePort = ((IPEndPoint)tempSocket.LocalEndPoint).Port;
                tempSocket.Close();
            }

            return availablePort;
        }
    }
}