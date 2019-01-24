using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
    /// <summary>
    /// Base Message Protocol Object
    /// </summary>
    public abstract class MessageProtocolObject
    {
        /// <summary>
        /// Every <see cref="MessageProtocolObject"/> has a token type, also it represents its starter byte at the token.
        /// </summary>
        public abstract TokenType TokenType { get; }
    }

    /// <summary>
    /// Simple string <see cref="MessageProtocolObject"/> that does not contains any <see cref="TokenType.Separator"/>
    /// </summary>
    public class StringProtocolObject: MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.String;

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Byte array <see cref="MessageProtocolObject"/>
    /// </summary>
    public class BlobProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Blob;

        /// <summary>
        /// Bytes
        /// </summary>
        public Memory<byte> Bytes { get; set; }
    }

    /// <summary>
    /// Error <see cref="MessageProtocolObject"/>
    /// </summary>
    public class ErrorProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Error;

        /// <summary>
        /// 8 bytes, ascii, error code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Utf8 encoded error message
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 32-bit signed integer <see cref="MessageProtocolObject"/>
    /// </summary>
    public class IntegerProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Integer;

        /// <summary>
        /// Value
        /// </summary>
        public int Value { get; set; }
    }
    
    /// <summary>
    /// 64-bit signed integer(long) <see cref="MessageProtocolObject"/>
    /// </summary>
    public class LongProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Long;

        /// <summary>
        /// Value
        /// </summary>
        public long Value { get; set; }
    }
    
    /// <summary>
    /// Double <see cref="MessageProtocolObject"/>
    /// </summary>
    public class DoubleProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Double;

        /// <summary>
        /// Value
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Null <see cref="MessageProtocolObject"/>
    /// </summary>
    public class NullProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Null;
    }

    /// <summary>
    /// Boolean <see cref="MessageProtocolObject"/>
    /// </summary>
    public class BooleanProtocolObject : MessageProtocolObject
    {
        /// <summary>
        /// Value
        /// </summary>
        public bool Value { get; set; }

        /// <inheritdoc />
        public override TokenType TokenType => Value ? TokenType.True : TokenType.False;
    }

    /// <summary>
    /// Array <see cref="MessageProtocolObject"/>
    /// </summary>
    public class ArrayProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Array;

        /// <summary>
        /// Items
        /// </summary>
        public Memory<MessageProtocolObject> Items { get; set; }
    }

    /// <summary>
    /// Map <see cref="MessageProtocolObject"/>
    /// </summary>
    public class MapProtocolObject : MessageProtocolObject
    {
        /// <inheritdoc />
        public override TokenType TokenType => TokenType.Map;

        /// <summary>
        /// Key-Value pairs
        /// </summary>
        public Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> Items { get; set; }
    }
}