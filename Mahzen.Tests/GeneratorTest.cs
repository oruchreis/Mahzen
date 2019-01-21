using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mahzen.Core;
using System.Linq;
using System.Diagnostics;

namespace Mahzen.Tests
{
    [TestClass]
    public class GeneratorTest
    {
        [TestMethod]
        public void WriteTest()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var expectedSimpleString = "ASĞPÜÜŞİÖÇçöçğüşi123*09dfgvnhsasq";
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
                    new StringProtocolObject
                    {
                        Value = expectedSimpleString
                    },
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

                Trace.WriteLine($"Generating stream: {stopWatch.Elapsed.TotalMilliseconds}ms");
                stopWatch.Restart();

                Memory<MessageProtocolObject> resultMem = new MessageProtocolObject[1024];                
                var parser = new Parser(memory.GetBuffer().AsSpan().Slice(0, (int)memory.Length), resultMem);

                Trace.WriteLine($"Stream Get Buffer: {stopWatch.Elapsed.TotalMilliseconds}ms");
                stopWatch.Restart();

                parser.Parse();
                var parsed = resultMem.Span;

                Trace.WriteLine($"Parse: {stopWatch.Elapsed.TotalMilliseconds}ms");
                stopWatch.Restart();

                Assert.AreEqual(0, parser.RemeaningBuffer.Length);

                var parsedString = parsed[0] as StringProtocolObject;
                Assert.IsInstanceOfType(parsed[0], typeof(StringProtocolObject));
                Assert.AreEqual(expectedSimpleString, parsedString.Value);

                var parsedBlob = parsed[1] as BlobProtocolObject;
                Assert.IsInstanceOfType(parsed[1], typeof(BlobProtocolObject));
                CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes.ToArray());

                var parsedError = parsed[2] as ErrorProtocolObject;
                Assert.IsInstanceOfType(parsed[2], typeof(ErrorProtocolObject));
                Assert.AreEqual(expectedErrorCode, parsedError.Code);
                Assert.AreEqual(expectedErrorMessage, parsedError.Message);
                
                var parsedInteger = parsed[3] as IntegerProtocolObject;
                Assert.IsInstanceOfType(parsed[3], typeof(IntegerProtocolObject));
                Assert.AreEqual(expectedInteger, parsedInteger.Value);

                var parsedLong = parsed[4] as LongProtocolObject;
                Assert.IsInstanceOfType(parsed[4], typeof(LongProtocolObject));
                Assert.AreEqual(expectedLong, parsedLong.Value);

                var parsedDouble = parsed[5] as DoubleProtocolObject;
                Assert.IsInstanceOfType(parsed[5], typeof(DoubleProtocolObject));
                Assert.AreEqual(expectedDouble, parsedDouble.Value);

                var parsedNull = parsed[6] as NullProtocolObject;
                Assert.IsInstanceOfType(parsed[6], typeof(NullProtocolObject));

                var parsedTrue = parsed[7] as BooleanProtocolObject;
                Assert.IsInstanceOfType(parsed[7], typeof(BooleanProtocolObject));
                Assert.AreEqual(true, parsedTrue.Value);

                var parsedFalse = parsed[8] as BooleanProtocolObject;
                Assert.IsInstanceOfType(parsed[8], typeof(BooleanProtocolObject));
                Assert.AreEqual(false, parsedFalse.Value);

                var parsedArray = parsed[9] as ArrayProtocolObject;
                Assert.IsInstanceOfType(parsed[9], typeof(ArrayProtocolObject));
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

                var parsedMap = parsed[10] as MapProtocolObject;
                Assert.IsInstanceOfType(parsed[10], typeof(MapProtocolObject));
                Assert.AreEqual(2, parsedMap.Items.Length);
                var keyValues = parsedMap.Items.ToArray();
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
