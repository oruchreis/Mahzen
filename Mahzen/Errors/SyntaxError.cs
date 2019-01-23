using System.Text;

namespace Mahzen.Core
{
    public class SyntaxError : Error
    {
        private const string _code = "00000001";

        public SyntaxError(string message) :
            base(message)
        {
        }

        public override string Code => _code;

    }

    public static class SyntaxErrorExceptionHelper
    {
        public static SyntaxError ToSyntaxError(this SyntaxErrorException e)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append(e.Message);
            var exp = e;
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
