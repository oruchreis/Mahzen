namespace Mahzen.Core
{
    /// <summary>
    /// Base Error class
    /// </summary>
    public abstract class Error
    {
        /// <summary>
        /// Every error has a unique code, smaller than 8 byte, only ascii chars
        /// </summary>
        public abstract string Code { get; }

        /// <summary>
        /// Formatted error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="message"></param>
        protected Error(string message)
        {
            Message = message;
        }
    }
}
