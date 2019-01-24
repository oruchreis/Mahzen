using System;
using Serilog.Events;

namespace Mahzen.Configuration
{
    class Settings
    {
        public static Settings Get;

        public NodeSettings Node { get; set; }
        public ClusterInitializationSettings ClusterInitialization { get; set; }
        public LoggingSettings Logging { get; set; }
        public LogEventLevel StdoutLevel { get; set; }
    }
}
