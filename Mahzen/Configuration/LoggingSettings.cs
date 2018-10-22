using System;
using Serilog;
using Serilog.Events;

namespace Mahzen.Configuration
{
    public class LoggingSettings
    {
        public bool IsEnabled { get; set; } = true;
        public string Path { get; set; } = "./logs";
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
        public long? RollingFileSizeBytes { get; set; } = null;
        public bool RollOnFileSize { get; set; } = false;
        public int? RetainedFileCount { get; set; } = 30;
    }
}
