using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mahzen.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mahzen.Tests
{
    [TestClass]
    public class ParserTest
    {
        /*
         * MMP Types:
         * - Blob:          $<length>\n<bytes>\n
         * - Errors:        !<length>\n<error_code>\n<error_message_utf8_string>\n
         * - Integer:       i<integer_4_bytes>\n
         * - Long:          l<long_8_bytes>\n
         * - Double:        d<double_8_bytes>\n
         * - Null:          N\n
         * - Boolean:       0\n or 1\n
         * - Array:         *<count>\n<items>        =>items can be any type
         * - Map:           %<count>\n<key><value>   =>key and values can be any type
         */

        private const char NewLine = '\n';
        
        [TestMethod]
        public void ReadBlob()
        {
            var expectedBlobData = Guid.NewGuid().ToByteArray();
            Span<byte> buffer = stackalloc byte[1 + sizeof(int) + 1 + expectedBlobData.Length + 1];
            
            using (var stream = new MemoryStream())
            using (var sw = new BinaryWriter(stream))
            {
                sw.Write((byte)TokenType.Blob);
                sw.Write(expectedBlobData.Length);
                sw.Write(NewLine);
                sw.Write(expectedBlobData);
                sw.Write(NewLine);
                stream.ToArray().CopyTo(buffer);
            }            
            
            var parser = new Parser(buffer);
            var result = parser.Parse();
            
            Assert.AreEqual(1, result.Count);
            var parsedBlobData = result[0] as BlobProtocolData; 
            Assert.IsNotNull(parsedBlobData);
            Assert.AreEqual(TokenType.Blob, parsedBlobData.TokenType);
            CollectionAssert.AreEqual(expectedBlobData, parsedBlobData.Bytes);
            Assert.AreEqual(0, parser.RemeaningBuffer.Length);
        }
        
        [TestMethod]
        public void ReadMultiple()
        {
            var expectedBlob = Guid.NewGuid().ToByteArray();
            var expectedErrorCode = Encoding.ASCII.GetBytes("00ABC123");
            var expectedErrorMessage = Encoding.UTF8.GetBytes("Deneme error message");
            var expectedInteger = new Random().Next();
            long expectedLong = new Random().Next() + int.MaxValue;
            double expectedDouble = new Random().NextDouble();
            
            Span<byte> buffer;
            
            using (var stream = new MemoryStream())
            using (var bw = new BinaryWriter(stream))
            {
                //blob
                bw.Write((byte)TokenType.Blob);
                bw.Write(expectedBlob.Length);
                bw.Write(NewLine);
                bw.Write(expectedBlob);
                bw.Write(NewLine);
                
                //error
                bw.Write((byte)TokenType.Error);
                bw.Write(expectedErrorMessage.Length);
                bw.Write(NewLine);
                bw.Write(expectedErrorCode);                
                bw.Write(NewLine);
                bw.Write(expectedErrorMessage);
                bw.Write(NewLine);
                
                //Integer
                bw.Write((byte)TokenType.Integer);
                bw.Write(expectedInteger);
                bw.Write(NewLine);
                
                //Long
                bw.Write((byte)TokenType.Long);
                bw.Write(expectedLong);
                bw.Write(NewLine);
                
                //Double
                bw.Write((byte)TokenType.Double);
                bw.Write(expectedDouble);
                bw.Write(NewLine);
                
                //Null
                bw.Write((byte)TokenType.Null);
                bw.Write(NewLine);
                
                //True
                bw.Write((byte)TokenType.True);
                bw.Write(NewLine);
                
                //False
                bw.Write((byte)TokenType.False);
                bw.Write(NewLine);
                
                //Array
                bw.Write((byte)TokenType.Array);
                bw.Write(3);
                bw.Write(NewLine);
                //ArrayItem:Blob
                bw.Write((byte)TokenType.Blob);
                bw.Write(expectedBlob.Length);
                bw.Write(NewLine);
                bw.Write(expectedBlob);
                bw.Write(NewLine);
                //ArrayItem:Integer
                bw.Write((byte)TokenType.Integer);
                bw.Write(expectedInteger);
                bw.Write(NewLine);
                //ArrayItem:Double
                bw.Write((byte)TokenType.Double);
                bw.Write(expectedDouble);
                bw.Write(NewLine);
                
                //Map
                bw.Write((byte)TokenType.Map);
                bw.Write(2);
                bw.Write(NewLine);
                //MapItem Key1: Blob
                bw.Write((byte)TokenType.Blob);
                bw.Write(expectedBlob.Length);
                bw.Write(NewLine);
                bw.Write(expectedBlob);
                bw.Write(NewLine);
                //MapItem Value1: Integer
                bw.Write((byte)TokenType.Integer);
                bw.Write(expectedInteger);
                bw.Write(NewLine);
                //MapItem Key2: Double
                bw.Write((byte)TokenType.Double);
                bw.Write(expectedDouble);
                bw.Write(NewLine);
                //MapItem Value2: Null
                bw.Write((byte)TokenType.Null);
                bw.Write(NewLine);
                
                buffer = stream.ToArray().AsSpan();
            }            
            
            var parser = new Parser(buffer);
            var result = parser.Parse();

            Assert.AreEqual(0, parser.RemeaningBuffer.Length);
            Assert.AreEqual(10, result.Count);

            var parsedBlob = result[0] as BlobProtocolData; 
            Assert.IsNotNull(parsedBlob);
            Assert.AreEqual(TokenType.Blob, parsedBlob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes);
            
            var parsedError = result[1] as ErrorProtocolData;
            Assert.IsNotNull(parsedError);
            Assert.AreEqual(TokenType.Error, parsedError.TokenType);
            CollectionAssert.AreEqual(expectedErrorCode, Encoding.ASCII.GetBytes(parsedError.Code));
            CollectionAssert.AreEqual(expectedErrorMessage, Encoding.UTF8.GetBytes(parsedError.Message));

            var parsedInteger = result[2] as IntegerProtocolData;
            Assert.IsNotNull(parsedInteger);
            Assert.AreEqual(TokenType.Integer, parsedInteger.TokenType);
            Assert.AreEqual(expectedInteger, parsedInteger.Value);

            var parsedLong = result[3] as LongProtocolData;
            Assert.IsNotNull(parsedLong);
            Assert.AreEqual(TokenType.Long, parsedLong.TokenType);
            Assert.AreEqual(expectedLong, parsedLong.Value);

            var parsedDouble = result[4] as DoubleProtocolData;
            Assert.IsNotNull(parsedDouble);
            Assert.AreEqual(TokenType.Double, parsedDouble.TokenType);
            Assert.AreEqual(expectedDouble, parsedDouble.Value);

            var parsedNull = result[5] as NullProtocolData;
            Assert.IsNotNull(parsedNull);
            Assert.AreEqual(TokenType.Null, parsedNull.TokenType);

            var parsedTrue = result[6] as BooleanProtocolData;
            Assert.IsNotNull(parsedTrue);
            Assert.AreEqual(TokenType.True, parsedTrue.TokenType);
            Assert.AreEqual(true, parsedTrue.Value);

            var parsedFalse = result[7] as BooleanProtocolData;
            Assert.IsNotNull(parsedFalse);
            Assert.AreEqual(TokenType.False, parsedFalse.TokenType);
            Assert.AreEqual(false, parsedFalse.Value);

            var parsedArray = result[8] as ArrayProtocolData;
            Assert.IsNotNull(parsedArray);
            Assert.AreEqual(TokenType.Array, parsedArray.TokenType);
            Assert.AreEqual(3, parsedArray.Items.Length);
            var parsedArrayBlob = parsedArray.Items[0] as BlobProtocolData;
            Assert.IsNotNull(parsedArrayBlob);
            Assert.AreEqual(TokenType.Blob, parsedArrayBlob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedArrayBlob.Bytes);
            var parsedArrayInteger = parsedArray.Items[1] as IntegerProtocolData;
            Assert.IsNotNull(parsedArrayInteger);
            Assert.AreEqual(TokenType.Integer, parsedArrayInteger.TokenType);
            Assert.AreEqual(expectedInteger, parsedArrayInteger.Value);
            var parsedArrayDouble = parsedArray.Items[2] as DoubleProtocolData;
            Assert.IsNotNull(parsedArrayDouble);
            Assert.AreEqual(TokenType.Double, parsedArrayDouble.TokenType);
            Assert.AreEqual(expectedDouble, parsedArrayDouble.Value);

            var parsedMap = result[9] as MapProtocolData;
            Assert.IsNotNull(parsedMap);
            Assert.AreEqual(TokenType.Map, parsedMap.TokenType);
            Assert.AreEqual(2, parsedMap.Items.Count);
            var keyValues = parsedMap.Items.ToArray();
            var parsedMapKey1Blob = keyValues[0].Key as BlobProtocolData;
            Assert.IsNotNull(parsedMapKey1Blob);
            Assert.AreEqual(TokenType.Blob, parsedMapKey1Blob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedMapKey1Blob.Bytes);
            var parsedMapValue1Integer = keyValues[0].Value as IntegerProtocolData;
            Assert.IsNotNull(parsedMapValue1Integer);
            Assert.AreEqual(TokenType.Integer, parsedMapValue1Integer.TokenType);
            Assert.AreEqual(expectedInteger, parsedMapValue1Integer.Value);
            var parsedMapKey2Double = keyValues[1].Key as DoubleProtocolData;
            Assert.IsNotNull(parsedMapKey2Double);
            Assert.AreEqual(TokenType.Double, parsedMapKey2Double.TokenType);
            Assert.AreEqual(expectedDouble, parsedMapKey2Double.Value);
            var parsedMapValue2Null = keyValues[1].Value as NullProtocolData;
            Assert.IsNotNull(parsedMapValue2Null);
            Assert.AreEqual(TokenType.Null, parsedMapValue2Null.TokenType);
        }

        [TestMethod]
        public void PartialParsing()
        {
            var expectedBlob = Guid.NewGuid().ToByteArray();
            var expectedErrorCode = "ABC123";
            var expectedErrorMessage = "Deneme error message";
            var expectedInteger = new Random().Next();
            long expectedLong = new Random().Next() + int.MaxValue;
            double expectedDouble = new Random().NextDouble();

            using (var memory = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memory))
            {
                binaryWriter.Write(
                    new BlobProtocolData
                    {
                        Bytes = expectedBlob
                    },
                    new ErrorProtocolData
                    {
                        Code = expectedErrorCode,
                        Message = expectedErrorMessage
                    },
                    new IntegerProtocolData
                    {
                        Value = expectedInteger
                    },
                    new LongProtocolData
                    {
                        Value = expectedLong
                    },
                    new DoubleProtocolData
                    {
                        Value = expectedDouble
                    },
                    new NullProtocolData(),
                    new BooleanProtocolData
                    {
                        Value = true
                    },
                    new BooleanProtocolData
                    {
                        Value = false
                    },
                    new ArrayProtocolData
                    {
                        Items = new MessageProtocolData[]
                        {
                            new BlobProtocolData
                            {
                                Bytes = expectedBlob
                            },
                            new IntegerProtocolData
                            {
                                Value = expectedInteger
                            },
                            new DoubleProtocolData
                            {
                                Value = expectedDouble
                            }
                        }
                    },
                    new MapProtocolData
                    {
                        Items = new Dictionary<MessageProtocolData, MessageProtocolData>
                        {
                            [new BlobProtocolData { Bytes = expectedBlob }] = new IntegerProtocolData { Value = expectedInteger },
                            [new DoubleProtocolData { Value = expectedDouble }] = new NullProtocolData()
                        }
                    });

                var buffer = memory.ToArray().AsSpan();
                var parser = new Parser(buffer.Slice(0, 50));
                var parsed = parser.Parse();

                Assert.AreEqual(27, parser.RemeaningBuffer.Length);

                var parsedBlob = parsed[0] as BlobProtocolData;
                Assert.IsInstanceOfType(parsed[0], typeof(BlobProtocolData));
                CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes);

                parser.Reset(buffer.Slice(50, 50));
                parsed = parser.Parse();

                Assert.AreEqual(9, parser.RemeaningBuffer.Length);

                var parsedError = parsed[0] as ErrorProtocolData;
                Assert.IsInstanceOfType(parsed[0], typeof(ErrorProtocolData));
                Assert.AreEqual(expectedErrorCode, parsedError.Code);
                Assert.AreEqual(expectedErrorMessage, parsedError.Message);

                var parsedInteger = parsed[1] as IntegerProtocolData;
                Assert.IsInstanceOfType(parsed[1], typeof(IntegerProtocolData));
                Assert.AreEqual(expectedInteger, parsedInteger.Value);

                var parsedLong = parsed[2] as LongProtocolData;
                Assert.IsInstanceOfType(parsed[2], typeof(LongProtocolData));
                Assert.AreEqual(expectedLong, parsedLong.Value);

                var parsedDouble = parsed[3] as DoubleProtocolData;
                Assert.IsInstanceOfType(parsed[3], typeof(DoubleProtocolData));
                Assert.AreEqual(expectedDouble, parsedDouble.Value);

                var parsedNull = parsed[4] as NullProtocolData;
                Assert.IsInstanceOfType(parsed[4], typeof(NullProtocolData));

                var parsedTrue = parsed[5] as BooleanProtocolData;
                Assert.IsInstanceOfType(parsed[5], typeof(BooleanProtocolData));
                Assert.AreEqual(true, parsedTrue.Value);

                var parsedFalse = parsed[6] as BooleanProtocolData;
                Assert.IsInstanceOfType(parsed[6], typeof(BooleanProtocolData));
                Assert.AreEqual(false, parsedFalse.Value);

                parser.Reset(buffer.Slice(100));
                parsed = parser.Parse();

                Assert.AreEqual(0, parser.RemeaningBuffer.Length);

                var parsedArray = parsed[0] as ArrayProtocolData;
                Assert.IsInstanceOfType(parsed[0], typeof(ArrayProtocolData));
                Assert.AreEqual(3, parsedArray.Items.Length);
                var parsedArrayBlob = parsedArray.Items[0] as BlobProtocolData;
                Assert.IsInstanceOfType(parsedArray.Items[0], typeof(BlobProtocolData));
                CollectionAssert.AreEqual(expectedBlob, parsedArrayBlob.Bytes);
                var parsedArrayInteger = parsedArray.Items[1] as IntegerProtocolData;
                Assert.IsInstanceOfType(parsedArray.Items[1], typeof(IntegerProtocolData));
                Assert.AreEqual(expectedInteger, parsedArrayInteger.Value);
                var parsedArrayDouble = parsedArray.Items[2] as DoubleProtocolData;
                Assert.IsInstanceOfType(parsedArray.Items[2], typeof(DoubleProtocolData));
                Assert.AreEqual(expectedDouble, parsedArrayDouble.Value);

                var parsedMap = parsed[1] as MapProtocolData;
                Assert.IsInstanceOfType(parsed[1], typeof(MapProtocolData));
                Assert.AreEqual(2, parsedMap.Items.Count);
                var keyValues = parsedMap.Items.ToArray();
                var parsedMapKey1Blob = keyValues[0].Key as BlobProtocolData;
                Assert.IsInstanceOfType(keyValues[0].Key, typeof(BlobProtocolData));
                CollectionAssert.AreEqual(expectedBlob, parsedMapKey1Blob.Bytes);
                var parsedMapValue1Integer = keyValues[0].Value as IntegerProtocolData;
                Assert.IsInstanceOfType(keyValues[0].Value, typeof(IntegerProtocolData));
                Assert.AreEqual(expectedInteger, parsedMapValue1Integer.Value);
                var parsedMapKey2Double = keyValues[1].Key as DoubleProtocolData;
                Assert.IsInstanceOfType(keyValues[1].Key, typeof(DoubleProtocolData));
                Assert.AreEqual(expectedDouble, parsedMapKey2Double.Value);
                var parsedMapValue2Null = keyValues[1].Value as NullProtocolData;
                Assert.IsInstanceOfType(keyValues[1].Value, typeof(NullProtocolData));
            }
        }
    }
}
