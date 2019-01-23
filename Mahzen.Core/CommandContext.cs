using System;
using System.Threading;

namespace Mahzen.Core
{
    /// <summary>
    /// Every invoking process of a command has a context which keeps required data.
    /// </summary>
    public class CommandContext: IDisposable
    {
        private static readonly AsyncLocal<CommandContext> _current;

        /// <summary>
        /// Current command context.
        /// </summary>
        public static CommandContext Current => _current.Value;

        /// <summary>
        /// Parent command context
        /// </summary>
        public readonly CommandContext Parent;

        /// <summary>
        /// Creates a command context, and sets this as the current context in the execution context.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="response"></param>
        public CommandContext(Command command, MessageProtocolBuilder response)
        {
            Parent = _current.Value;
            _current.Value = this;
            Command = command;
            Response = response;
        }

        /// <summary>
        /// Current invoking command in the context
        /// </summary>
        public Command Command { get; }

        /// <summary>
        /// Response of the command.
        /// </summary>
        public MessageProtocolBuilder Response { get; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _current.Value = Parent;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CommandContext() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
