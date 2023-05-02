﻿using ReverseProxy.Core.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Interfaces
{
    public interface IServerUriProvider
    {
        public ConcurrentDictionary<string, Uri> GetServerUris();
    }
}
