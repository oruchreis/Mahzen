using System;
using System.Text;

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

        public SyntaxErrorException(string message, int position, SyntaxErrorException innerException)
            : base(string.Format(message, position), innerException)
        {
            Position = position;
        }

        public SyntaxError ToSyntaxError()
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append(Message);
            var exp = this;
            var indent = 1;
            while ((exp = exp.InnerException as SyntaxErrorException) != null)
            {
                strBuilder.AppendLine();
                strBuilder.Append(new string('\t', indent * 2));
                strBuilder.Append(exp.Message);
                indent++;
            }
            
            return new SyntaxError(strBuilder.ToString());
        }
    }
}