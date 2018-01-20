using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public class AppServiceObject
    {
        public class Properties
        {
            public string serverFarmId { get; set; }

            public string siteConfig { get; set; }
        }

        public string kind { get; set; }

        public Properties properties { get; set; }

        public string location { get; set; }

        public string tags { get; set; }
    }
}
