using System;
using Serilog.Events;

namespace Mahzen.Configuration
{
    public class Settings
    {
        public NodeSettings Node { get; set; }
        public ClusterInitializationSettings ClusterInitialization { get; set; }
        public LoggingSettings Logging { get; set; }
        public LogEventLevel StdoutLevel { get; set; }
    }
}
