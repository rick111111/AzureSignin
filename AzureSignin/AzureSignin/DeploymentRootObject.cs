using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Test.Apex.VisualStudio.Debugger.Tests.SnapshotDebugger
{
    public class DeploymentTemplateObject
    {
        public DeploymentRootProperties properties { get; set; }
    }

    public class DeploymentRootProperties
    {
        internal const string TemplateJsonString = @"
{
  ""$schema"": ""http://schema.management.azure.com/schemas/2014-04-01-preview/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {
    ""name"": {
      ""type"": ""String""
    },
    ""hostingPlanName"": {
      ""type"": ""String""
    },
    ""hostingEnvironment"": {
      ""type"": ""String""
    },
    ""location"": {
      ""type"": ""String""
    },
    ""sku"": {
      ""type"": ""String""
    },
    ""skuCode"": {
      ""type"": ""String""
    },
    ""workerSize"": {
      ""type"": ""String""
    },
    ""serverFarmResourceGroup"": {
      ""type"": ""String""
    },
    ""subscriptionId"": {
      ""type"": ""String""
    }
  },
  ""resources"": [
    {
      ""type"": ""Microsoft.Web/sites"",
      ""name"": ""[parameters('name')]"",
      ""apiVersion"": ""2016-03-01"",
      ""location"": ""[parameters('location')]"",
      ""tags"": {
        ""[concat('hidden-related:', '/subscriptions/', parameters('subscriptionId'),'/resourcegroups/', parameters('serverFarmResourceGroup'), '/providers/Microsoft.Web/serverfarms/', parameters('hostingPlanName'))]"": ""empty""
      },
      ""properties"": {
        ""name"": ""[parameters('name')]"",
        ""serverFarmId"": ""[concat('/subscriptions/', parameters('subscriptionId'),'/resourcegroups/', parameters('serverFarmResourceGroup'), '/providers/Microsoft.Web/serverfarms/', parameters('hostingPlanName'))]"",
        ""hostingEnvironment"": ""[parameters('hostingEnvironment')]""
      },
      ""dependsOn"": [
        ""[concat('Microsoft.Web/serverfarms/', parameters('hostingPlanName'))]""
      ]
    },
    {
      ""type"": ""Microsoft.Web/serverfarms"",
      ""sku"": {
        ""Tier"": ""[parameters('sku')]"",
        ""Name"": ""[parameters('skuCode')]""
      },
      ""name"": ""[parameters('hostingPlanName')]"",
      ""apiVersion"": ""2016-09-01"",
      ""location"": ""[parameters('location')]"",
      ""properties"": {
        ""name"": ""[parameters('hostingPlanName')]"",
        ""workerSizeId"": ""[parameters('workerSize')]"",
        ""reserved"": false,
        ""numberOfWorkers"": ""1"",
        ""hostingEnvironment"": ""[parameters('hostingEnvironment')]""
      }
    }
  ]
}
";           

        public object template { get; set; }

        public DeploymentParameterObject parameters { get; set; }

        public string mode { get; set; } = "Complete";
    }

    public class Location
    {
        public string value { get; set; }
    }

    public class HostingEnvironment
    {
        public string value { get; set; }
    }

    public class SkuCode
    {
        public string value { get; set; }
    }

    public class Name
    {
        public string value { get; set; }
    }

    public class Sku
    {
        public string value { get; set; }
    }

    public class ServerFarmResourceGroup
    {
        public string value { get; set; }
    }

    public class WorkerSize
    {
        public string value { get; set; }
    }

    public class HostingPlanName
    {
        public string value { get; set; }
    }

    public class SubscriptionId
    {
        public string value { get; set; }
    }

    public class DeploymentParameterObject
    {
        public Location location { get; set; } = new Location() { value = "West US 2" };

        public HostingEnvironment hostingEnvironment { get; set; } = new HostingEnvironment() { value = "" };

        public SkuCode skuCode { get; set; } = new SkuCode() { value = "S1" };

        public Name name { get; set; }

        public Sku sku { get; set; } = new Sku() { value = "Standard" };

        public ServerFarmResourceGroup serverFarmResourceGroup { get; set; }

        public WorkerSize workerSize { get; set; } = new WorkerSize() { value = "0" };

        public HostingPlanName hostingPlanName { get; set; }

        public SubscriptionId subscriptionId { get; set; }
    }
}
