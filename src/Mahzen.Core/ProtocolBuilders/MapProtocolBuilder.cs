using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
    /// <summary>
    /// Using for creating map protocol objects by begin-end method.
    /// </summary>
    public class MapProtocolBuilder: IDisposable
    {
        private readonly Action<Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>> _endMapAction;
        private readonly MapItemProtocolBuilder _mapKeyProtocolBuilder;
        private readonly MapItemProtocolBuilder _mapValueProtocolBuilder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endMapAction"></param>
        public MapProtocolBuilder(Action<Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>> endMapAction)
        {
            _endMapAction = endMapAction;
            _mapKeyProtocolBuilder = new MapItemProtocolBuilder();
            _mapValueProtocolBuilder = new MapItemProtocolBuilder();
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
                    EndMap();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MapProtocolBuilder() {
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

        private readonly List<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> _items = new List<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>();

        /// <summary>
        /// Gets the items that created so far.
        /// </summary>
        /// <returns></returns>
        public Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> GetItems() => _items.ToArray();

        /// <summary>
        /// Ends the map object
        /// </summary>
        public void EndMap()
        {
            _endMapAction?.Invoke(GetItems());
        }
        
        /// <summary>
        /// Writes key-value pair using protocol builders.
        /// </summary>
        /// <param name="keyBuilder"></param>
        /// <param name="valueBuilder"></param>
        public void Write(Action<IProtocolBuilder> keyBuilder, Action<IProtocolBuilder> valueBuilder)
        {
            keyBuilder(_mapKeyProtocolBuilder);
            valueBuilder(_mapValueProtocolBuilder);

            _items.Add(new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(_mapKeyProtocolBuilder.ProtocolObject, _mapValueProtocolBuilder.ProtocolObject));
        }
    }
}
