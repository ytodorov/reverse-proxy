using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class RoundRobinLoadBalancerStrategy : ILoadBalancerStrategy
    {
        private readonly IReadOnlyList<Uri> _serverUris;
        private static int _currentServerIndex = 0;

        public RoundRobinLoadBalancerStrategy(IServerUriProvider serverUriProvider)
        {
            _serverUris = serverUriProvider.GetServerUris();
        }

        public Uri GetNextServerUri()
        {
            var serverUri = _serverUris[_currentServerIndex];
            // Important to handle many concurrent request
            Interlocked.Increment(ref _currentServerIndex);
            _currentServerIndex = _currentServerIndex % _serverUris.Count;

            return serverUri;
        }
    }
}
