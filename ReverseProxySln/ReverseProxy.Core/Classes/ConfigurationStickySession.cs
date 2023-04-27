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
    public class ConfigurationStickySession : IStickySession
    {
        private readonly IConfiguration _configuration;

        public ConfigurationStickySession(IConfiguration configuration)
        {
            _configuration = configuration;
        }
       
        public bool IsStickySessionEnabled()
        {
            var stickySessionSection = _configuration.GetSection("LoadBalancer:EnableStickySession");
            if (stickySessionSection == null)
            {
                return false;
            }
            
            var isStickySessionEnabled = stickySessionSection.Get<bool>();
            return isStickySessionEnabled;

        }
    }
}
