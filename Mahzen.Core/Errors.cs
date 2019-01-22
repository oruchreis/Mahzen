using System;
using System.Collections.Generic;
using System.Text;

namespace Mahzen.Core
{
    public abstract class Error
    {
        public abstract string Code { get; }
        public string Message { get; }

        protected Error(string message)
        {
            Message = message;
        }
    }

    public class SyntaxError : Error
    {
        private const string _code = "00000001";

        public SyntaxError(string message) :
            base(message)
        {
        }

        public override string Code => _code;

    }

    public class UnknownCommandError : Error
    {
        private const string _code = "00000002";

        private const string _defaultMessage = "Unknown Command {0}";

        public UnknownCommandError(string unknownCommand) : 
            base(string.Format(_defaultMessage, unknownCommand))
        {
        }

        public override string Code => _code;
    }

    public class InternalError : Error
    {
        private const string _defaultMessage = "Internal error occured.";

        public InternalError(string internalCode)
            : base(_defaultMessage)
        {
            Code = $"I{internalCode,-7}";
        }

        public override string Code { get; }
    }
}
