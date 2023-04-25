using ReverseProxy.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Classes
{
    public class StickySessionDisabled : IStickySession
    {
        public bool IsStickySessionEnabled()
        {
            return false;
        }
    }
}
