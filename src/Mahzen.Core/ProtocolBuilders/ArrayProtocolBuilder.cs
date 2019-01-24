using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
    /// <summary>
    /// Using for creating array protocol objects
    /// </summary>
    public class ArrayProtocolBuilder : ProtocolBuilder, IDisposable
    {
        private readonly List<MessageProtocolObject> _items = new List<MessageProtocolObject>();

        /// <summary>
        /// Gets the created items so far.
        /// </summary>
        /// <returns></returns>
        public Memory<MessageProtocolObject> GetItems() => _items.ToArray();

        /// <inheritdoc />
        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            _items.Add(protocolObject);
        }

        /// <summary>
        /// 
        /// </summary>
        public ArrayProtocolBuilder()
        {

        }

        private readonly Action<Memory<MessageProtocolObject>> _endArrayAction;

        /// <summary>
        /// Using for begin-end method.
        /// </summary>
        /// <param name="endArrayAction"></param>
        public ArrayProtocolBuilder(Action<Memory<MessageProtocolObject>> endArrayAction)
        {
            _endArrayAction = endArrayAction;
        }

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
                    EndArray();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ArrayProtocolBuilder() {
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

        /// <summary>
        /// Ends the array if called with <see cref="ProtocolBuilder.BeginArray"/>
        /// </summary>
        public void EndArray()
        {
            _endArrayAction?.Invoke(GetItems());
        }
    }
}
