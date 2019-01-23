using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /// <summary>
    /// Protocol Builder Interface
    /// </summary>
    public interface IProtocolBuilder
    {
        /// <summary>
        /// Writes string value 
        /// </summary>
        /// <param name="value"></param>
        void Write(string value);

        /// <summary>
        /// Writes integer value
        /// </summary>
        /// <param name="value"></param>
        void Write(int value);

        /// <summary>
        /// Writes double value
        /// </summary>
        /// <param name="value"></param>
        void Write(double value);

        /// <summary>
        /// Writes long value
        /// </summary>
        /// <param name="value"></param>
        void Write(long value);

        /// <summary>
        /// Writes boolean value
        /// </summary>
        /// <param name="value"></param>
        void Write(bool value);

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="error"></param>
        void WriteError(Error error);

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        void WriteError(string errorCode, string errorMessage);

        /// <summary>
        /// Write null value
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Writes an array.
        /// </summary>
        /// <param name="arrayItemBuilders"></param>
        void Write(params Action<IProtocolBuilder>[] arrayItemBuilders);

        /// <summary>
        /// Writes a map.
        /// </summary>
        /// <param name="mapItemBuilders"></param>
        void Write(params (Action<IProtocolBuilder> KeyBuilder, Action<IProtocolBuilder> ValueBuilder)[] mapItemBuilders);

        /// <summary>
        /// Helper method to create an array by begin-end methods. Can be used with using.
        /// </summary>
        /// <returns></returns>
        ArrayProtocolBuilder BeginArray();

        /// <summary>
        /// Helper method to create a map by begin-end methods. Can be used with using.
        /// </summary>
        /// <returns></returns>
        MapProtocolBuilder BeginMap();

    }

    /// <summary>
    /// Base protocol builder which implements basic implementations.
    /// </summary>
    public abstract class ProtocolBuilder : IProtocolBuilder
    {
        /// <summary>
        /// Handles the writing of <see cref="MessageProtocolObject"/>
        /// </summary>
        /// <param name="protocolObject"></param>
        protected abstract void HandleWrite(MessageProtocolObject protocolObject);

        /// <inheritdoc />
        public virtual void Write(string value)
        {
            HandleWrite(new StringProtocolObject { Value = value });
        }

        /// <inheritdoc />
        public virtual void Write(int value)
        {
            HandleWrite(new IntegerProtocolObject { Value = value });
        }

        /// <inheritdoc />
        public virtual void Write(double value)
        {
            HandleWrite(new DoubleProtocolObject { Value = value });
        }

        /// <inheritdoc />
        public virtual void Write(long value)
        {
            HandleWrite(new LongProtocolObject { Value = value });
        }

        /// <inheritdoc />
        public virtual void Write(bool value)
        {
            HandleWrite(new BooleanProtocolObject { Value = value });
        }

        /// <inheritdoc />
        public virtual void WriteError(Error error)
        {
            WriteError(error.Code, error.Message);
        }

        /// <inheritdoc />
        public virtual void WriteError(string errorCode, string errorMessage)
        {
            HandleWrite(new ErrorProtocolObject { Code = errorCode, Message = errorMessage });
        }

        /// <inheritdoc />
        public virtual void WriteNull()
        {
            HandleWrite(new NullProtocolObject());
        }

        /// <inheritdoc />
        public virtual void Write(params Action<IProtocolBuilder>[] arrayItemBuilders)
        {
            var arrayProtocolBuilder = new ArrayProtocolBuilder();
            foreach (var action in arrayItemBuilders)
            {
                action(arrayProtocolBuilder);
            }
            HandleWrite(new ArrayProtocolObject { Items = arrayProtocolBuilder.GetItems() });
        }

        /// <inheritdoc />
        public virtual void Write(params (Action<IProtocolBuilder> KeyBuilder, Action<IProtocolBuilder> ValueBuilder)[] mapItemBuilders)
        {
            var mapKeyProtocolBuilder = new MapItemProtocolBuilder();
            var mapValueProtocolBuilder = new MapItemProtocolBuilder();

            var items = new List<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>();
            foreach (var (KeyBuilder, ValueBuilder) in mapItemBuilders)
            {
                KeyBuilder(mapKeyProtocolBuilder);
                ValueBuilder(mapValueProtocolBuilder);
                items.Add(new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(mapKeyProtocolBuilder.ProtocolObject, mapValueProtocolBuilder.ProtocolObject));
            }

            HandleWrite(new MapProtocolObject { Items = items.ToArray() });
        }

        /// <inheritdoc />
        public virtual ArrayProtocolBuilder BeginArray()
        {
            return new ArrayProtocolBuilder(items => HandleWrite(new ArrayProtocolObject{ Items = items }));
        }

        /// <inheritdoc />
        public virtual MapProtocolBuilder BeginMap()
        {
            return new MapProtocolBuilder(items => HandleWrite(new MapProtocolObject { Items = items }));
        }
    }

    /// <summary>
    /// The Protocol Builder that writes message protocols to an output stream.
    /// </summary>
    public class MessageProtocolBuilder : ProtocolBuilder, IDisposable
    {
        //binarywriter will block writes, so we must use faster stream to write then flush this memory stream to output stream.
        private readonly MemoryStream _internalBuffer = new MemoryStream();
        private readonly BinaryWriter _binaryWriter;
        private readonly Stream _outputStream;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputStream"></param>
        public MessageProtocolBuilder(Stream outputStream)
        {
            _binaryWriter = new BinaryWriter(_internalBuffer, Encoding.UTF8);
            _outputStream = outputStream;
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
                    _internalBuffer?.Dispose();
                    _binaryWriter?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageProtocolBuilder() {
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

        /// <inheritdoc />
        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            _binaryWriter.Write(protocolObject);
        }

        /// <summary>
        /// Write all internal buffer to the output stream, and clears the internal buffer.
        /// </summary>
        /// <returns></returns>
        public async Task FlushAsync()
        {
            _internalBuffer.Position = 0;
            await _internalBuffer.CopyToAsync(_outputStream).ConfigureAwait(false);
            _internalBuffer.SetLength(0);
        }

        /// <summary>
        /// Write all internal buffer to the output stream, and clears the internal buffer.
        /// </summary>
        public void Flush()
        {
            _internalBuffer.Position = 0;
            _internalBuffer.CopyTo(_outputStream);
            _internalBuffer.SetLength(0);
        }
    }

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

    /// <summary>
    /// Using for creating map protocol object items
    /// </summary>
    public class MapItemProtocolBuilder : ProtocolBuilder
    {
        /// <summary>
        /// Created map item
        /// </summary>
        public MessageProtocolObject ProtocolObject { get; set; }

        /// <inheritdoc />
        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            ProtocolObject = protocolObject;
        }
    }

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
