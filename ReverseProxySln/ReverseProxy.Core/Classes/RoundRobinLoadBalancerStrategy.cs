using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class RoundRobinLoadBalancerStrategy : ILoadBalancerStrategy
    {
        private readonly IReadOnlyList<UriWithHash> _serverUris;
        private static int _currentServerIndex = 0;        

        public RoundRobinLoadBalancerStrategy(IServerUriProvider serverUriProvider)
        {
            _serverUris = serverUriProvider.GetServerUris();
        }

        public UriWithHash GetNextServerUri()
        {
            var serverUri = _serverUris[_currentServerIndex];
            // Important to handle many concurrent request
            Interlocked.Increment(ref _currentServerIndex);
            _currentServerIndex = _currentServerIndex % _serverUris.Count;

            return serverUri;
        }
    }
}
