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
        private const char NewLine = '\n';

        [TestMethod]
        public void ReadString()
        {
            var expectedString1 = "ASDğĞSDÇŞÖşüğşüöasd";
            var expectedString2 = "ASDğĞ SDÇŞÖş  üğşüöasd";
            var expectedString3 = "ASDğĞ SDÇŞÖş  üğşüöasd";

            using (var stream = new MemoryStream())
            using (var bw = new BinaryWriter(stream))
            {
                bw.Write(
                    new StringProtocolObject { Value = expectedString1 },
                    new StringProtocolObject { Value = expectedString2 },
                    new StringProtocolObject { Value = expectedString3 }
                    );

                Memory<MessageProtocolObject> result = new MessageProtocolObject[1024];
                var parser = new Parser(stream.GetBuffer().AsSpan().Slice(0, (int)stream.Length), result);
                parser.Parse();
                var parsed = result.Span;
                Assert.IsInstanceOfType(parsed[0], typeof(StringProtocolObject));
                Assert.AreEqual(expectedString1, (parsed[0] as StringProtocolObject)?.Value);
                Assert.IsInstanceOfType(parsed[1], typeof(StringProtocolObject));
                Assert.AreEqual(expectedString2, (parsed[1] as StringProtocolObject)?.Value);
                Assert.IsInstanceOfType(parsed[2], typeof(StringProtocolObject));
                Assert.AreEqual(expectedString3, (parsed[2] as StringProtocolObject)?.Value);
            }
        }

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
            Memory<MessageProtocolObject> result = new MessageProtocolObject[1024];
            var parser = new Parser(buffer, result);
            parser.Parse();
            var resultSpan = result.Span.Slice(0, parser.ResultIndex);
            Assert.AreEqual(1, resultSpan.Length);
            var parsedBlobData = resultSpan[0] as BlobProtocolObject; 
            Assert.IsNotNull(parsedBlobData);
            Assert.AreEqual(TokenType.Blob, parsedBlobData.TokenType);
            CollectionAssert.AreEqual(expectedBlobData, parsedBlobData.Bytes.ToArray());
            Assert.AreEqual(0, parser.RemeaningBuffer.Length);
        }
        
        [TestMethod]
        public void ReadMultiple()
        {
            var expectedSimpleString = "ASĞPÜÜŞİÖÇçöçğüşi123*09dfgvnhsasq";
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
                bw.Write((byte)TokenType.String);
                bw.Write(Encoding.UTF8.GetBytes(expectedSimpleString));
                bw.Write(NewLine);
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

            Memory<MessageProtocolObject> resultMem = new MessageProtocolObject[1024];
            var parser = new Parser(buffer, resultMem);
            parser.Parse();

            var result = resultMem.Span.Slice(0, parser.ResultIndex);

            Assert.AreEqual(0, parser.RemeaningBuffer.Length);
            Assert.AreEqual(11, result.Length);

            var parsedString = result[0] as StringProtocolObject;
            Assert.IsInstanceOfType(result[0], typeof(StringProtocolObject));
            Assert.AreEqual(expectedSimpleString, parsedString.Value);

            var parsedBlob = result[1] as BlobProtocolObject; 
            Assert.IsNotNull(parsedBlob);
            Assert.AreEqual(TokenType.Blob, parsedBlob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes.ToArray());
            
            var parsedError = result[2] as ErrorProtocolObject;
            Assert.IsNotNull(parsedError);
            Assert.AreEqual(TokenType.Error, parsedError.TokenType);
            CollectionAssert.AreEqual(expectedErrorCode, Encoding.ASCII.GetBytes(parsedError.Code));
            CollectionAssert.AreEqual(expectedErrorMessage, Encoding.UTF8.GetBytes(parsedError.Message));

            var parsedInteger = result[3] as IntegerProtocolObject;
            Assert.IsNotNull(parsedInteger);
            Assert.AreEqual(TokenType.Integer, parsedInteger.TokenType);
            Assert.AreEqual(expectedInteger, parsedInteger.Value);

            var parsedLong = result[4] as LongProtocolObject;
            Assert.IsNotNull(parsedLong);
            Assert.AreEqual(TokenType.Long, parsedLong.TokenType);
            Assert.AreEqual(expectedLong, parsedLong.Value);

            var parsedDouble = result[5] as DoubleProtocolObject;
            Assert.IsNotNull(parsedDouble);
            Assert.AreEqual(TokenType.Double, parsedDouble.TokenType);
            Assert.AreEqual(expectedDouble, parsedDouble.Value);

            var parsedNull = result[6] as NullProtocolObject;
            Assert.IsNotNull(parsedNull);
            Assert.AreEqual(TokenType.Null, parsedNull.TokenType);

            var parsedTrue = result[7] as BooleanProtocolObject;
            Assert.IsNotNull(parsedTrue);
            Assert.AreEqual(TokenType.True, parsedTrue.TokenType);
            Assert.AreEqual(true, parsedTrue.Value);

            var parsedFalse = result[8] as BooleanProtocolObject;
            Assert.IsNotNull(parsedFalse);
            Assert.AreEqual(TokenType.False, parsedFalse.TokenType);
            Assert.AreEqual(false, parsedFalse.Value);

            var parsedArray = result[9] as ArrayProtocolObject;
            Assert.IsNotNull(parsedArray);
            Assert.AreEqual(TokenType.Array, parsedArray.TokenType);
            Assert.AreEqual(3, parsedArray.Items.Length);
            var parsedArrayItems = parsedArray.Items.Span;
            var parsedArrayBlob = parsedArrayItems[0] as BlobProtocolObject;
            Assert.IsNotNull(parsedArrayBlob);
            Assert.AreEqual(TokenType.Blob, parsedArrayBlob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedArrayBlob.Bytes.ToArray());
            var parsedArrayInteger = parsedArrayItems[1] as IntegerProtocolObject;
            Assert.IsNotNull(parsedArrayInteger);
            Assert.AreEqual(TokenType.Integer, parsedArrayInteger.TokenType);
            Assert.AreEqual(expectedInteger, parsedArrayInteger.Value);
            var parsedArrayDouble = parsedArrayItems[2] as DoubleProtocolObject;
            Assert.IsNotNull(parsedArrayDouble);
            Assert.AreEqual(TokenType.Double, parsedArrayDouble.TokenType);
            Assert.AreEqual(expectedDouble, parsedArrayDouble.Value);

            var parsedMap = result[10] as MapProtocolObject;
            Assert.IsNotNull(parsedMap);
            Assert.AreEqual(TokenType.Map, parsedMap.TokenType);
            Assert.AreEqual(2, parsedMap.Items.Length);
            var keyValues = parsedMap.Items.Span;
            var parsedMapKey1Blob = keyValues[0].Key as BlobProtocolObject;
            Assert.IsNotNull(parsedMapKey1Blob);
            Assert.AreEqual(TokenType.Blob, parsedMapKey1Blob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedMapKey1Blob.Bytes.ToArray());
            var parsedMapValue1Integer = keyValues[0].Value as IntegerProtocolObject;
            Assert.IsNotNull(parsedMapValue1Integer);
            Assert.AreEqual(TokenType.Integer, parsedMapValue1Integer.TokenType);
            Assert.AreEqual(expectedInteger, parsedMapValue1Integer.Value);
            var parsedMapKey2Double = keyValues[1].Key as DoubleProtocolObject;
            Assert.IsNotNull(parsedMapKey2Double);
            Assert.AreEqual(TokenType.Double, parsedMapKey2Double.TokenType);
            Assert.AreEqual(expectedDouble, parsedMapKey2Double.Value);
            var parsedMapValue2Null = keyValues[1].Value as NullProtocolObject;
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
                    new BlobProtocolObject
                    {
                        Bytes = expectedBlob
                    },
                    new ErrorProtocolObject
                    {
                        Code = expectedErrorCode,
                        Message = expectedErrorMessage
                    },
                    new IntegerProtocolObject
                    {
                        Value = expectedInteger
                    },
                    new LongProtocolObject
                    {
                        Value = expectedLong
                    },
                    new DoubleProtocolObject
                    {
                        Value = expectedDouble
                    },
                    new NullProtocolObject(),
                    new BooleanProtocolObject
                    {
                        Value = true
                    },
                    new BooleanProtocolObject
                    {
                        Value = false
                    },
                    new ArrayProtocolObject
                    {
                        Items = new MessageProtocolObject[]
                        {
                            new BlobProtocolObject
                            {
                                Bytes = expectedBlob
                            },
                            new IntegerProtocolObject
                            {
                                Value = expectedInteger
                            },
                            new DoubleProtocolObject
                            {
                                Value = expectedDouble
                            }
                        }
                    },
                    new MapProtocolObject
                    {
                        Items = new KeyValuePair<MessageProtocolObject, MessageProtocolObject>[]
                        {
                            new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(new BlobProtocolObject { Bytes = expectedBlob }, new IntegerProtocolObject { Value = expectedInteger }),
                            new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(new DoubleProtocolObject { Value = expectedDouble }, new NullProtocolObject())
                        }
                    });

                var buffer = memory.ToArray().AsSpan();
                Memory<MessageProtocolObject> resultMem = new MessageProtocolObject[1024];                
                var parser = new Parser(buffer.Slice(0, 50), resultMem);
                parser.Parse();

                var parsed = resultMem.Span.Slice(0, parser.ResultIndex);

                Assert.AreEqual(27, parser.RemeaningBuffer.Length);

                var parsedBlob = parsed[0] as BlobProtocolObject;
                Assert.IsInstanceOfType(parsed[0], typeof(BlobProtocolObject));
                CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes.ToArray());

                parser.SlideBuffer(buffer.Slice(50, 50));
                parser.Parse();
                parsed = resultMem.Span.Slice(0, parser.ResultIndex);

                Assert.AreEqual(9, parser.RemeaningBuffer.Length);

                var parsedError = parsed[1] as ErrorProtocolObject;
                Assert.IsInstanceOfType(parsed[1], typeof(ErrorProtocolObject));
                Assert.AreEqual(expectedErrorCode, parsedError.Code);
                Assert.AreEqual(expectedErrorMessage, parsedError.Message);

                var parsedInteger = parsed[2] as IntegerProtocolObject;
                Assert.IsInstanceOfType(parsed[2], typeof(IntegerProtocolObject));
                Assert.AreEqual(expectedInteger, parsedInteger.Value);

                var parsedLong = parsed[3] as LongProtocolObject;
                Assert.IsInstanceOfType(parsed[3], typeof(LongProtocolObject));
                Assert.AreEqual(expectedLong, parsedLong.Value);

                var parsedDouble = parsed[4] as DoubleProtocolObject;
                Assert.IsInstanceOfType(parsed[4], typeof(DoubleProtocolObject));
                Assert.AreEqual(expectedDouble, parsedDouble.Value);

                var parsedNull = parsed[5] as NullProtocolObject;
                Assert.IsInstanceOfType(parsed[5], typeof(NullProtocolObject));

                var parsedTrue = parsed[6] as BooleanProtocolObject;
                Assert.IsInstanceOfType(parsed[6], typeof(BooleanProtocolObject));
                Assert.AreEqual(true, parsedTrue.Value);

                var parsedFalse = parsed[7] as BooleanProtocolObject;
                Assert.IsInstanceOfType(parsed[7], typeof(BooleanProtocolObject));
                Assert.AreEqual(false, parsedFalse.Value);

                parser.SlideBuffer(buffer.Slice(100));
                parser.Parse();
                parsed = resultMem.Span.Slice(0, parser.ResultIndex);

                Assert.AreEqual(0, parser.RemeaningBuffer.Length);

                var parsedArray = parsed[8] as ArrayProtocolObject;
                Assert.IsInstanceOfType(parsed[8], typeof(ArrayProtocolObject));
                Assert.AreEqual(3, parsedArray.Items.Length);
                var parsedArrayItems = parsedArray.Items.Span;
                var parsedArrayBlob = parsedArrayItems[0] as BlobProtocolObject;
                Assert.IsInstanceOfType(parsedArrayItems[0], typeof(BlobProtocolObject));
                CollectionAssert.AreEqual(expectedBlob, parsedArrayBlob.Bytes.ToArray());
                var parsedArrayInteger = parsedArrayItems[1] as IntegerProtocolObject;
                Assert.IsInstanceOfType(parsedArrayItems[1], typeof(IntegerProtocolObject));
                Assert.AreEqual(expectedInteger, parsedArrayInteger.Value);
                var parsedArrayDouble = parsedArrayItems[2] as DoubleProtocolObject;
                Assert.IsInstanceOfType(parsedArrayItems[2], typeof(DoubleProtocolObject));
                Assert.AreEqual(expectedDouble, parsedArrayDouble.Value);

                var parsedMap = parsed[9] as MapProtocolObject;
                Assert.IsInstanceOfType(parsed[9], typeof(MapProtocolObject));
                Assert.AreEqual(2, parsedMap.Items.Length);
                var keyValues = parsedMap.Items.Span;
                var parsedMapKey1Blob = keyValues[0].Key as BlobProtocolObject;
                Assert.IsInstanceOfType(keyValues[0].Key, typeof(BlobProtocolObject));
                CollectionAssert.AreEqual(expectedBlob, parsedMapKey1Blob.Bytes.ToArray());
                var parsedMapValue1Integer = keyValues[0].Value as IntegerProtocolObject;
                Assert.IsInstanceOfType(keyValues[0].Value, typeof(IntegerProtocolObject));
                Assert.AreEqual(expectedInteger, parsedMapValue1Integer.Value);
                var parsedMapKey2Double = keyValues[1].Key as DoubleProtocolObject;
                Assert.IsInstanceOfType(keyValues[1].Key, typeof(DoubleProtocolObject));
                Assert.AreEqual(expectedDouble, parsedMapKey2Double.Value);
                var parsedMapValue2Null = keyValues[1].Value as NullProtocolObject;
                Assert.IsInstanceOfType(keyValues[1].Value, typeof(NullProtocolObject));
            }
        }
    }
}
