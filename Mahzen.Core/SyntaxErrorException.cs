using System;

namespace Mahzen.Core
{
    public class SyntaxErrorException: Exception
    {
        public int Position { get; }
        
        public SyntaxErrorException(string message, int position)
            : base(string.Format(message, position))
        {
            Position = position;
        }
    }
}