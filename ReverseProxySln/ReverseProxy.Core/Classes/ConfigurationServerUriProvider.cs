using Microsoft.Extensions.Configuration;
using ReverseProxy.Core.Extensions;
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

        public IReadOnlyList<UriWithHash> GetServerUris()
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
            var serverUris = new List<UriWithHash>(serverUriStrings.Count);
            foreach (var uriString in serverUriStrings)
            {
                if (!Uri.TryCreate(uriString, UriKind.Absolute, out var serverUri))
                {
                    throw new InvalidOperationException($"Invalid URI '{uriString}' in the 'LoadBalancer:ServerUris' section.");
                }
                UriWithHash uriWithHash = new UriWithHash()
                {
                    Uri = serverUri,
                    Hash = serverUri.ToString().CalculateSHA256()
                };

                serverUris.Add(uriWithHash);
            }

            return serverUris.AsReadOnly();
        }
    }
}
