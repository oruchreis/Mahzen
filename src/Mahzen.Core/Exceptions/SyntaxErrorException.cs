using System;
using System.Text;

namespace Mahzen.Core
{
    /// <summary>
    /// Represents a syntax error
    /// </summary>
    public class SyntaxErrorException: Exception
    {
        /// <summary>
        /// Position in the source
        /// </summary>
        public int Position { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="position"></param>
        public SyntaxErrorException(string message, int position)
            : base(string.Format(message, position))
        {
            Position = position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="position"></param>
        /// <param name="innerException"></param>
        public SyntaxErrorException(string message, int position, SyntaxErrorException innerException)
            : base(string.Format(message, position), innerException)
        {
            Position = position;
        }
    }
}