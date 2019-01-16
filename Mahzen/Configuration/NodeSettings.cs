using System;
namespace Mahzen.Configuration
{
    public class NodeSettings
    {
        public string ListenIpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 6970;
        public int NodeTalkPort { get; set; } = 8100;
    }
}
