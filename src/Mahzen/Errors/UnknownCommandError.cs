namespace Mahzen.Core
{
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
}
