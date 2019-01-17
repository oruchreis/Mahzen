using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mahzen.Core;
using System.Linq;

namespace Mahzen.Tests
{
    [TestClass]
    public class GeneratorTest
    {
        [TestMethod]
        public void WriteTest()
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

                var parser = new Parser(memory.ToArray().AsSpan());
                var parsed = parser.Parse();
                Assert.AreEqual(0, parser.RemeaningBuffer.Length);
                
                var parsedBlob = parsed[0] as BlobProtocolData;
                Assert.IsInstanceOfType(parsed[0], typeof(BlobProtocolData));
                CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes);

                var parsedError = parsed[1] as ErrorProtocolData;
                Assert.IsInstanceOfType(parsed[1], typeof(ErrorProtocolData));
                Assert.AreEqual(expectedErrorCode, parsedError.Code);
                Assert.AreEqual(expectedErrorMessage, parsedError.Message);
                
                var parsedInteger = parsed[2] as IntegerProtocolData;
                Assert.IsInstanceOfType(parsed[2], typeof(IntegerProtocolData));
                Assert.AreEqual(expectedInteger, parsedInteger.Value);

                var parsedLong = parsed[3] as LongProtocolData;
                Assert.IsInstanceOfType(parsed[3], typeof(LongProtocolData));
                Assert.AreEqual(expectedLong, parsedLong.Value);

                var parsedDouble = parsed[4] as DoubleProtocolData;
                Assert.IsInstanceOfType(parsed[4], typeof(DoubleProtocolData));
                Assert.AreEqual(expectedDouble, parsedDouble.Value);

                var parsedNull = parsed[5] as NullProtocolData;
                Assert.IsInstanceOfType(parsed[5], typeof(NullProtocolData));

                var parsedTrue = parsed[6] as BooleanProtocolData;
                Assert.IsInstanceOfType(parsed[6], typeof(BooleanProtocolData));
                Assert.AreEqual(true, parsedTrue.Value);

                var parsedFalse = parsed[7] as BooleanProtocolData;
                Assert.IsInstanceOfType(parsed[7], typeof(BooleanProtocolData));
                Assert.AreEqual(false, parsedFalse.Value);

                var parsedArray = parsed[8] as ArrayProtocolData;
                Assert.IsInstanceOfType(parsed[8], typeof(ArrayProtocolData));
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

                var parsedMap = parsed[9] as MapProtocolData;
                Assert.IsInstanceOfType(parsed[9], typeof(MapProtocolData));
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
