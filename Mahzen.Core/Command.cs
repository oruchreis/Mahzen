using System;

namespace Mahzen.Core
{
    /// <summary>
    /// Represents a command
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Command's unique keyword
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// Parameters of the command.
        /// </summary>
        public Memory<MessageProtocolObject> Parameters { get; set; }
    }
}