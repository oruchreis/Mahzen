using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mahzen.Core
{
    public static class GeneratorHelper
    {
        private const char NewLine = '\n';

        public static void Write(this BinaryWriter binaryWriter, params MessageProtocolData[] datas)
        {
            foreach (var data in datas)
            {
                //first byte is the token type.
                binaryWriter.Write((byte)data.TokenType);

                switch (data)
                {
                    case BlobProtocolData blobProtocolData:
                        binaryWriter.Write(blobProtocolData.Bytes.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(blobProtocolData.Bytes);
                        binaryWriter.Write(NewLine);
                        break;
                    case ErrorProtocolData errorProtocolData:
                        var codeBytes = Encoding.ASCII.GetBytes(errorProtocolData.Code);
                        if (codeBytes.Length < 8)
                            Array.Resize(ref codeBytes, 8);
                        if (codeBytes.Length > 8)
                            throw new InvalidOperationException($"Error code is bigger than 8 bytes: {errorProtocolData.Code}");

                        var messageBytes = Encoding.UTF8.GetBytes(errorProtocolData.Message);

                        binaryWriter.Write(messageBytes.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(codeBytes);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(messageBytes);
                        binaryWriter.Write(NewLine);
                        break;
                    case IntegerProtocolData integerProtocolData:
                        binaryWriter.Write(integerProtocolData.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case LongProtocolData longProtocolData:
                        binaryWriter.Write(longProtocolData.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case DoubleProtocolData doubleProtocolData:
                        binaryWriter.Write(doubleProtocolData.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case NullProtocolData nullProtocolData:
                    case BooleanProtocolData booleanProtocolData:
                        binaryWriter.Write(NewLine);
                        break;
                    case ArrayProtocolData arrayProtocolData:
                        binaryWriter.Write(arrayProtocolData.Items.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(arrayProtocolData.Items);
                        break;
                    case MapProtocolData mapProtocolData:
                        binaryWriter.Write(mapProtocolData.Items.Count);
                        binaryWriter.Write(NewLine);
                        foreach (var kv in mapProtocolData.Items)
                        {
                            binaryWriter.Write(kv.Key);
                            binaryWriter.Write(kv.Value);
                        }
                        break;
                    default:
                        throw new NotSupportedException("Not supported message protocol data type.");
                }
            }
        }
    }
}
