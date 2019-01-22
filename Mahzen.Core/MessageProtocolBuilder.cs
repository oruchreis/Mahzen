using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    public interface IProtocolBuilder
    {
        void Write(string value);
        void Write(int value);
        void Write(double value);
        void Write(long value);
        void Write(bool value);
        void WriteError(string errorCode, string errorMessage);
        void WriteNull();
        void Write(params Action<IProtocolBuilder>[] arrayItemBuilders);
        void Write(params (Action<IProtocolBuilder> KeyBuilder, Action<IProtocolBuilder> ValueBuilder)[] mapItemBuilders);
        ArrayProtocolBuilder BeginArray();
        MapProtocolBuilder BeginMap();

    }

    public abstract class ProtocolBuilder : IProtocolBuilder
    {
        protected abstract void HandleWrite(MessageProtocolObject protocolObject);

        public virtual void Write(string value)
        {
            HandleWrite(new StringProtocolObject { Value = value });
        }

        public virtual void Write(int value)
        {
            HandleWrite(new IntegerProtocolObject { Value = value });
        }

        public virtual void Write(double value)
        {
            HandleWrite(new DoubleProtocolObject { Value = value });
        }

        public virtual void Write(long value)
        {
            HandleWrite(new LongProtocolObject { Value = value });
        }

        public virtual void Write(bool value)
        {
            HandleWrite(new BooleanProtocolObject { Value = value });
        }

        public virtual void WriteError(Error error)
        {
            WriteError(error.Code, error.Message);
        }

        public virtual void WriteError(string errorCode, string errorMessage)
        {
            HandleWrite(new ErrorProtocolObject { Code = errorCode, Message = errorMessage });
        }

        public virtual void WriteNull()
        {
            HandleWrite(new NullProtocolObject());
        }

        public virtual void Write(params Action<IProtocolBuilder>[] arrayItemBuilders)
        {
            var arrayProtocolBuilder = new ArrayProtocolBuilder();
            foreach (var action in arrayItemBuilders)
            {
                action(arrayProtocolBuilder);
            }
            HandleWrite(new ArrayProtocolObject { Items = arrayProtocolBuilder.GetItems() });
        }

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

        public virtual ArrayProtocolBuilder BeginArray()
        {
            return new ArrayProtocolBuilder(items => HandleWrite(new ArrayProtocolObject{ Items = items }));
        }

        public virtual MapProtocolBuilder BeginMap()
        {
            return new MapProtocolBuilder(items => HandleWrite(new MapProtocolObject { Items = items }));
        }
    }

    public class MessageProtocolBuilder : ProtocolBuilder, IDisposable
    {
        //binarywriter will block writes, so we must use faster stream to write then flush this memory stream to output stream.
        private readonly MemoryStream _internalBuffer = new MemoryStream();
        private readonly BinaryWriter _binaryWriter;
        private readonly Stream _outputStream;

        public MessageProtocolBuilder(Stream outputStream)
        {
            _binaryWriter = new BinaryWriter(_internalBuffer, Encoding.UTF8);
            _outputStream = outputStream;
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

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

    public class ArrayProtocolBuilder : ProtocolBuilder, IDisposable
    {
        private readonly List<MessageProtocolObject> _items = new List<MessageProtocolObject>();

        public Memory<MessageProtocolObject> GetItems() => _items.ToArray();

        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            _items.Add(protocolObject);
        }

        public ArrayProtocolBuilder()
        {

        }

        private readonly Action<Memory<MessageProtocolObject>> _endArrayAction;
        public ArrayProtocolBuilder(Action<Memory<MessageProtocolObject>> endArrayAction)
        {
            _endArrayAction = endArrayAction;
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public void EndArray()
        {
            _endArrayAction?.Invoke(GetItems());
        }
    }

    public class MapItemProtocolBuilder : ProtocolBuilder
    {
        public MessageProtocolObject ProtocolObject { get; set; }

        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            ProtocolObject = protocolObject;
        }
    }

    public class MapProtocolBuilder: IDisposable
    {
        private readonly Action<Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>> _endMapAction;
        private readonly MapItemProtocolBuilder _mapKeyProtocolBuilder;
        private readonly MapItemProtocolBuilder _mapValueProtocolBuilder;

        public MapProtocolBuilder(Action<Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>> endMapAction)
        {
            _endMapAction = endMapAction;
            _mapKeyProtocolBuilder = new MapItemProtocolBuilder();
            _mapValueProtocolBuilder = new MapItemProtocolBuilder();
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        private readonly List<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> _items = new List<KeyValuePair<MessageProtocolObject, MessageProtocolObject>>();
        public Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> GetItems() => _items.ToArray();

        public void EndMap()
        {
            _endMapAction?.Invoke(GetItems());
        }
        
        public void Write(Action<IProtocolBuilder> keyBuilder, Action<IProtocolBuilder> valueBuilder)
        {
            keyBuilder(_mapKeyProtocolBuilder);
            valueBuilder(_mapValueProtocolBuilder);

            _items.Add(new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(_mapKeyProtocolBuilder.ProtocolObject, _mapValueProtocolBuilder.ProtocolObject));
        }
    }
}
