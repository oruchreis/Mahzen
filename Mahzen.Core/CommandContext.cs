﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Mahzen.Core
{
    public class CommandContext: IDisposable
    {
        private static readonly AsyncLocal<CommandContext> _current;

        public static CommandContext Current => _current.Value;

        public CommandContext(Command command, MessageProtocolBuilder response)
        {
            _current.Value = this;
            Command = command;
            Response = response;
        }

        public Command Command { get; }
        public MessageProtocolBuilder Response { get; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _current.Value = null;
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
