using System;

namespace Mahzen.Core
{
    public class Command
    {
        public string Keyword { get; set; }
        public Memory<MessageProtocolObject> Parameters { get; set; }
    }
}