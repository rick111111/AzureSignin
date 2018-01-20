using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public class ServicePlanObject
    {
        public class Sku
        {
            public string name { get; set; }

            public string tier { get; set; }

            public string size { get; set; }
        }

        public string kind { get; set; }

        public Sku sku { get; set; }

        public string location { get; set; }

        public string tags { get; set; }
    }
}
