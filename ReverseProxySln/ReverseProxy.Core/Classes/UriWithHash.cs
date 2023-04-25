using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class UriWithHash
    {
        public Uri? Uri { get; set; }

        /// <summary>
        /// Use this hash for session affinity
        /// </summary>
        public string? Hash { get; set; }
    }
}
