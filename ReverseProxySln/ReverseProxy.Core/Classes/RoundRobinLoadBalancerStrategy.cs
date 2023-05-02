using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class RoundRobinLoadBalancerStrategy : ILoadBalancerStrategy
    {
        private readonly ConcurrentDictionary<string, Uri> _serverUris;
        private static int _currentServerIndex = 0;        

        public RoundRobinLoadBalancerStrategy(IServerUriProvider serverUriProvider)
        {
            _serverUris = serverUriProvider.GetServerUris();
        }

        public Uri GetNextServerUri()
        {
            if (_currentServerIndex >= _serverUris.Count)
            {
                _currentServerIndex = _currentServerIndex % _serverUris.Count;
            }

            var serverUri = _serverUris.ElementAt(_currentServerIndex).Value;

            // Important to handle many concurrent requests
            Interlocked.Increment(ref _currentServerIndex);
            _currentServerIndex = _currentServerIndex % _serverUris.Count;

            return serverUri;
        }
    }
}
