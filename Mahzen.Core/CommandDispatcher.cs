using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    public abstract class CommandDispatcher : IDisposable
    {
        private static readonly AsyncLocal<CommandDispatcher> _current = new AsyncLocal<CommandDispatcher>();
        private static readonly List<ICommandInvoker> _invokers = new List<ICommandInvoker>();
        private static readonly ReaderWriterLockSlim _invokersLocker = new ReaderWriterLockSlim();
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

        public static ICollection<ICommandInvoker> GetInvokers()
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

        protected readonly Stream Stream;
        protected readonly CancellationToken CancelToken;
        protected readonly MessageProtocolBuilder Response;
        protected readonly CommandDispatcher ParentDispatcher;

        public CommandDispatcher(Stream stream, in CancellationToken cancelToken)
        {
            ParentDispatcher = _current.Value;
            _current.Value = this;
            Stream = stream;
            CancelToken = cancelToken;
            Response = new MessageProtocolBuilder(Stream);
        }

        public abstract Task HandleAsync();

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

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

        // This code added to correctly implement the disposable pattern.
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