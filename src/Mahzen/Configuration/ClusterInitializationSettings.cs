using System;
using System.Collections.Generic;

namespace Mahzen.Configuration
{
    public class ClusterInitializationSettings
    {
        public string ClusterName { get; set; } = "Mahzen";
        public List<string> Nodes { get; set; }
        public string MasterAmount { get; set; } = "50%";
    }
}
