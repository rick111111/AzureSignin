using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public class ResourceGroupObject
    {
        public string id { get; set; }

        public string name { get; set; }

        public string location { get; set; }
    }
}
