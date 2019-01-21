using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mahzen.Core
{
    public static class GeneratorHelper
    {
        private const char NewLine = '\n';

        public static void Write(this BinaryWriter binaryWriter, params MessageProtocolObject[] datas)
        {
            Write(binaryWriter, datas.AsSpan());
        }

        public static void Write(this BinaryWriter binaryWriter, Span<MessageProtocolObject> datas)
        {
            foreach (var data in datas)
            {
                //first byte is the token type.
                binaryWriter.Write((byte)data.TokenType);

                switch (data)
                {
                    case StringProtocolObject stringProtocolObject:
                        binaryWriter.Write(Encoding.UTF8.GetBytes(stringProtocolObject.Value));
                        binaryWriter.Write(NewLine);
                        break;
                    case BlobProtocolObject blobProtocolObject:
                        binaryWriter.Write(blobProtocolObject.Bytes.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(blobProtocolObject.Bytes.Span);
                        binaryWriter.Write(NewLine);
                        break;
                    case ErrorProtocolObject errorProtocolObject:
                        var codeBytes = Encoding.ASCII.GetBytes(errorProtocolObject.Code);
                        if (codeBytes.Length < 8)
                            Array.Resize(ref codeBytes, 8);
                        if (codeBytes.Length > 8)
                            throw new InvalidOperationException($"Error code is bigger than 8 bytes: {errorProtocolObject.Code}");

                        var messageBytes = Encoding.UTF8.GetBytes(errorProtocolObject.Message);

                        binaryWriter.Write(messageBytes.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(codeBytes);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(messageBytes);
                        binaryWriter.Write(NewLine);
                        break;
                    case IntegerProtocolObject integerProtocolObject:
                        binaryWriter.Write(integerProtocolObject.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case LongProtocolObject longProtocolObject:
                        binaryWriter.Write(longProtocolObject.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case DoubleProtocolObject doubleProtocolObject:
                        binaryWriter.Write(doubleProtocolObject.Value);
                        binaryWriter.Write(NewLine);
                        break;
                    case NullProtocolObject nullProtocolObject:
                    case BooleanProtocolObject booleanProtocolObject:
                        binaryWriter.Write(NewLine);
                        break;
                    case ArrayProtocolObject arrayProtocolObject:
                        binaryWriter.Write(arrayProtocolObject.Items.Length);
                        binaryWriter.Write(NewLine);
                        binaryWriter.Write(arrayProtocolObject.Items.Span);
                        break;
                    case MapProtocolObject mapProtocolObject:
                        binaryWriter.Write(mapProtocolObject.Items.Length);
                        binaryWriter.Write(NewLine);
                        foreach (var kv in mapProtocolObject.Items.Span)
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
