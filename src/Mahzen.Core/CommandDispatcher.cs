using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /// <summary>
    /// Keeps and registers command invokers, gets commands over a stream, and dispatches the commands to the related invoker.
    /// </summary>
    public abstract class CommandDispatcher : IDisposable
    {
        private static readonly AsyncLocal<CommandDispatcher> _current = new AsyncLocal<CommandDispatcher>();
        private static readonly List<ICommandInvoker> _invokers = new List<ICommandInvoker>();
        private static readonly ReaderWriterLockSlim _invokersLocker = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets current dispatcher in the execution context.
        /// </summary>
        public static CommandDispatcher Current => _current.Value;

        /// <summary>
        /// Registers command invokers.
        /// </summary>
        /// <param name="invokers"></param>
        public static void RegisterInvoker(params ICommandInvoker[] invokers)
        {
            _invokersLocker.EnterWriteLock();
            try
            {
                _invokers.AddRange(invokers);
            }
            finally
            {
                _invokersLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the registered invokers. It will use a read lock, so use wisely.
        /// </summary>
        /// <returns></returns>
        public static ICommandInvoker[] GetInvokers()
        {
            _invokersLocker.EnterReadLock();
            try
            {
                return _invokers.ToArray();
            }
            finally
            {
                _invokersLocker.ExitReadLock();
            }
        }

        /// <summary>
        /// Associated stream which keeps the unparsed commands.
        /// </summary>
        protected readonly Stream Stream;

        /// <summary>
        /// Cancellation Token 
        /// </summary>
        protected readonly CancellationToken CancelToken;

        /// <summary>
        /// Response stream.
        /// </summary>
        protected readonly MessageProtocolBuilder Response;

        /// <summary>
        /// The parent dispatcher if there is any.
        /// </summary>
        protected readonly CommandDispatcher ParentDispatcher;

        /// <summary>
        /// Creates a command dispatcher. Dont forget to dispose it.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancelToken"></param>
        public CommandDispatcher(Stream stream, in CancellationToken cancelToken)
        {
            ParentDispatcher = _current.Value;
            _current.Value = this;
            Stream = stream;
            CancelToken = cancelToken;
            Response = new MessageProtocolBuilder(Stream);
        }

        /// <summary>
        /// Gets command requests from the stream, and dispatches the commands to the registered invokers.
        /// </summary>
        /// <returns></returns>
        public abstract Task HandleAsync();

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

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
                    _current.Value = ParentDispatcher;
                    Response?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CommandDispatcher() {
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