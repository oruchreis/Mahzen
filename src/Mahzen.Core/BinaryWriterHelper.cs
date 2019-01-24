#if !NET472
using System;
using System.IO;
using System.Text;

namespace Mahzen.Core
{
    /// <summary>
    /// BinaryWriter helpers to work with <see cref="MessageProtocolObject"/>
    /// </summary>
    public static class BinaryWriterHelper
    {
        private const byte Separator = (byte) TokenType.Separator;

        /// <summary>
        /// Writes multiple <see cref="MessageProtocolObject"/> to the <paramref name="binaryWriter"/>
        /// </summary>
        /// <param name="binaryWriter"></param>
        /// <param name="datas"></param>
        public static void Write(this BinaryWriter binaryWriter, params MessageProtocolObject[] datas)
        {
            Write(binaryWriter, datas.AsSpan());
        }

        /// <summary>
        /// Writes multiple <see cref="MessageProtocolObject"/> to the <paramref name="binaryWriter"/>
        /// </summary>
        /// <param name="binaryWriter"></param>
        /// <param name="datas"></param>
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
                        binaryWriter.Write(Separator);
                        break;
                    case BlobProtocolObject blobProtocolObject:
                        binaryWriter.Write(blobProtocolObject.Bytes.Length);
                        binaryWriter.Write(Separator);
                        binaryWriter.Write(blobProtocolObject.Bytes.Span);
                        binaryWriter.Write(Separator);
                        break;
                    case ErrorProtocolObject errorProtocolObject:
                        var codeBytes = Encoding.ASCII.GetBytes(errorProtocolObject.Code);
                        if (codeBytes.Length < 8)
                            Array.Resize(ref codeBytes, 8);
                        if (codeBytes.Length > 8)
                            throw new InvalidOperationException($"Error code is bigger than 8 bytes: {errorProtocolObject.Code}");

                        var messageBytes = Encoding.UTF8.GetBytes(errorProtocolObject.Message);

                        binaryWriter.Write(messageBytes.Length);
                        binaryWriter.Write(Separator);
                        binaryWriter.Write(codeBytes);
                        binaryWriter.Write(Separator);
                        binaryWriter.Write(messageBytes);
                        binaryWriter.Write(Separator);
                        break;
                    case IntegerProtocolObject integerProtocolObject:
                        binaryWriter.Write(integerProtocolObject.Value);
                        binaryWriter.Write(Separator);
                        break;
                    case LongProtocolObject longProtocolObject:
                        binaryWriter.Write(longProtocolObject.Value);
                        binaryWriter.Write(Separator);
                        break;
                    case DoubleProtocolObject doubleProtocolObject:
                        binaryWriter.Write(doubleProtocolObject.Value);
                        binaryWriter.Write(Separator);
                        break;
                    case NullProtocolObject nullProtocolObject:
                    case BooleanProtocolObject booleanProtocolObject:
                        binaryWriter.Write(Separator);
                        break;
                    case ArrayProtocolObject arrayProtocolObject:
                        binaryWriter.Write(arrayProtocolObject.Items.Length);
                        binaryWriter.Write(Separator);
                        binaryWriter.Write(arrayProtocolObject.Items.Span);
                        break;
                    case MapProtocolObject mapProtocolObject:
                        binaryWriter.Write(mapProtocolObject.Items.Length);
                        binaryWriter.Write(Separator);
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
#endif