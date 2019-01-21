using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
    public abstract class MessageProtocolObject
    {
        public abstract TokenType TokenType { get; }
    }

    public class StringProtocolObject: MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.String;
        public string Value { get; set; }
    }

    public class BlobProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Blob;
        public Memory<byte> Bytes { get; set; }
    }

    public class ErrorProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Error;
        public string Code { get; set; } //8byte
        public string Message { get; set; }
    }

    public class IntegerProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Integer;
        public int Value { get; set; }
    }
    
    public class LongProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Long;
        public long Value { get; set; }
    }
    
    public class DoubleProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Double;
        public double Value { get; set; }
    }

    public class NullProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Null;
    }

    public class BooleanProtocolObject : MessageProtocolObject
    {
        public bool Value { get; set; }
        public override TokenType TokenType => Value ? TokenType.True : TokenType.False;
    }

    public class ArrayProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Array;
        public Memory<MessageProtocolObject> Items { get; set; }
    }

    public class MapProtocolObject : MessageProtocolObject
    {
        public override TokenType TokenType => TokenType.Map;
        public Memory<KeyValuePair<MessageProtocolObject, MessageProtocolObject>> Items { get; set; }
    }
}