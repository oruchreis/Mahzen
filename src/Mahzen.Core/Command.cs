using System;

namespace Mahzen.Core
{
    /// <summary>
    /// Represents a command
    /// </summary>
    public class Command
    {
        public Command(string keyword)
        {
            Keyword = keyword;
            Parameters = Memory<MessageProtocolObject>.Empty;
        }

        public Command(string keyword, Memory<MessageProtocolObject> parameters)
        {
            Keyword = Keyword;
            Parameters = parameters;
        }

        /// <summary>
        /// Command's unique keyword
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Parameters of the command.
        /// </summary>
        public Memory<MessageProtocolObject> Parameters { get; }
    }
}