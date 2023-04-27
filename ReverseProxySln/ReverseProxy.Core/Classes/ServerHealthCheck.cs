using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class ServerHealthCheck : IHealthCheck, IDisposable
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IServerUriProvider _serverUriProvider;
        private readonly Timer _timer;
        private readonly int _healthCheckIntervalMilliseconds;

        private ConcurrentDictionary<string, Uri> _serverUris;

        public ServerHealthCheck(
            IHttpClientFactory clientFactory,
            IServerUriProvider serverUriProvider,
            int healthCheckIntervalMilliseconds = 1000)
        {
            _clientFactory = clientFactory;
            _serverUriProvider = serverUriProvider;
            _healthCheckIntervalMilliseconds = healthCheckIntervalMilliseconds;

            var initialServerUris = _serverUriProvider.GetServerUris();
            _serverUris = new ConcurrentDictionary<string, Uri>(initialServerUris);

            // Start the timer with the specified interval for health checks
            _timer = new Timer(HealthCheckCallback, null, _healthCheckIntervalMilliseconds, Timeout.Infinite);
        }

        private async void HealthCheckCallback(object state)
        {
            try
            {
                await CheckServerHealth();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
            }
            finally
            {
                _timer.Change(_healthCheckIntervalMilliseconds, Timeout.Infinite);
            }
        }

        private async Task CheckServerHealth()
        {
            // It is important always to get the all server uris so we can check if unavailable uri becomes available
            var initialServerUris = _serverUriProvider.GetServerUris();

            _serverUris = new ConcurrentDictionary<string, Uri>(initialServerUris);
            var httpClient = _clientFactory.CreateClient(nameof(ServerHealthCheck));

            foreach (var serverUri in _serverUris.Values.ToList())
            {
                try
                {
                    // Here we can set a specific endpoint for healt checks
                    // var healthCheckUri = new Uri(serverUri, "/healthcheck");  This is just an example
                    var healthCheckUri = new Uri(serverUri, "/");

                    var response = await httpClient.GetAsync(healthCheckUri);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Server {serverUri} is healthy");
                    }
                    else
                    {
                        Console.WriteLine($"Server {serverUri} is unhealthy, status code: {response.StatusCode}");
                        RemoveServerUri(serverUri);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to check server health at {serverUri}: {ex.Message}");
                    RemoveServerUri(serverUri);
                }
            }
        }

        private void RemoveServerUri(Uri serverUri)
        {
            var key = _serverUris.FirstOrDefault(x => x.Value == serverUri).Key;
            if (key != null)
            {
                _serverUris.TryRemove(key, out _);
            }
        }

        public IReadOnlyDictionary<string, Uri> GetHealthyServerUris()
        {
            return new ReadOnlyDictionary<string, Uri>(_serverUris);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
