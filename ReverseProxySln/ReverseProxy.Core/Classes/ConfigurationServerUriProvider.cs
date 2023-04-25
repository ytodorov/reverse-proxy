using Microsoft.Extensions.Configuration;
using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class ConfigurationServerUriProvider : IServerUriProvider
    {
        private readonly IConfiguration _configuration;

        public ConfigurationServerUriProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IReadOnlyList<Uri> GetServerUris()
        {
            var serverUrisSection = _configuration.GetSection("LoadBalancer:ServerUris");
            if (serverUrisSection == null)
            {
                throw new InvalidOperationException("Missing 'LoadBalancer:ServerUris' section in the configuration.");
            }

            var serverUriStrings = serverUrisSection.Get<List<string>>();
            if (serverUriStrings == null || serverUriStrings.Count == 0)
            {
                throw new InvalidOperationException("No server URIs configured in the 'LoadBalancer:ServerUris' section.");
            }

            var serverUris = new List<Uri>(serverUriStrings.Count);
            foreach (var uriString in serverUriStrings)
            {
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var serverUri))
                {
                    throw new InvalidOperationException($"Invalid URI '{uriString}' in the 'LoadBalancer:ServerUris' section.");
                }

                serverUris.Add(serverUri);
            }

            return serverUris.AsReadOnly();
        }
    }
}
