using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
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
}
