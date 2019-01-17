using System;
using System.Collections.Generic;

namespace Mahzen.Core
{
    public abstract class MessageProtocolData
    {
        public abstract TokenType TokenType { get; }
    }

    public class BlobProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Blob;
        public byte[] Bytes { get; set; }
    }

    public class ErrorProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Error;
        public string Code { get; set; } //8byte
        public string Message { get; set; }
    }

    public class IntegerProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Integer;
        public int Value { get; set; }
    }
    
    public class LongProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Long;
        public long Value { get; set; }
    }
    
    public class DoubleProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Double;
        public double Value { get; set; }
    }

    public class NullProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Null;
    }

    public class BooleanProtocolData : MessageProtocolData
    {
        public bool Value { get; set; }
        public override TokenType TokenType => Value ? TokenType.True : TokenType.False;
    }

    public class ArrayProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Array;
        public MessageProtocolData[] Items { get; set; }
    }

    public class MapProtocolData : MessageProtocolData
    {
        public override TokenType TokenType => TokenType.Map;
        public Dictionary<MessageProtocolData, MessageProtocolData> Items { get; set; }
    }
}