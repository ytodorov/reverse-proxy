using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Interfaces
{
    public interface IHealthCheck
    {
        IReadOnlyDictionary<string, Uri> GetHealthyServerUris();
    }
}
