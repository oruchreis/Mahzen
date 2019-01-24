namespace Mahzen.Core
{
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
