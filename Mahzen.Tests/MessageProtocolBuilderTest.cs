using Mahzen.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Tests
{
    [TestClass]
    public class MessageProtocolBuilderTest
    {
        [TestMethod]
        public async Task BuilderDeserialize()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var builder = new MessageProtocolBuilder(memoryStream))
                {
                    builder.Write(
                        ab => ab.Write("GET"),
                        ab => ab.Write("key1"),
                        ab => ab.Write(true),
                        ab => ab.Write(5),
                        ab => ab.Write(1.2d),
                        ab => ab.Write((long)int.MaxValue + 1),
                        ab => ab.WriteError("errorcod", "error message"));

                    await builder.FlushAsync();
                    Assert.IsTrue(memoryStream.Length > 0);
                }

                memoryStream.Position = 0;
                var commands = await CommandSerializer.DeserializeAsync(memoryStream);

                Assert.AreEqual(1, commands.Count);
                var command = commands[0];
                Assert.AreEqual("GET", command.Keyword);
                Assert.AreEqual("key1", (command.Parameters.Span[0] as StringProtocolObject)?.Value);
                Assert.AreEqual(true, (command.Parameters.Span[1] as BooleanProtocolObject)?.Value);
                Assert.AreEqual(5, (command.Parameters.Span[2] as IntegerProtocolObject)?.Value);
                Assert.AreEqual(1.2d, (command.Parameters.Span[3] as DoubleProtocolObject)?.Value);
                Assert.AreEqual((long)int.MaxValue + 1, (command.Parameters.Span[4] as LongProtocolObject)?.Value);
                Assert.AreEqual("errorcod", (command.Parameters.Span[5] as ErrorProtocolObject)?.Code);
                Assert.AreEqual("error message", (command.Parameters.Span[5] as ErrorProtocolObject)?.Message);
            }
        }

        [TestMethod]
        public void ArrayMapNested()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var builder = new MessageProtocolBuilder(memoryStream))
                {
                    using (var arrayBuilder = builder.BeginArray())
                    {
                        arrayBuilder.Write("SET");
                        using (var mapBuilder = arrayBuilder.BeginMap())
                        {
                            mapBuilder.Write(b => b.Write("key1"), b => b.Write("value1"));
                            mapBuilder.Write(b => b.Write("key2"), b => b.Write("value2"));
                            mapBuilder.Write(b => b.Write("key3"), b =>
                            {
                                using(var nestedArrayBuilder = b.BeginArray())
                                {
                                    nestedArrayBuilder.Write(true);
                                    nestedArrayBuilder.WriteNull();
                                }
                            });
                        }
                    }

                    builder.Flush();
                    Assert.IsTrue(memoryStream.Length > 0);
                }

                memoryStream.Position = 0;
                Memory<MessageProtocolObject> result = new MessageProtocolObject[1024];
                var parser = new Parser(memoryStream.GetBuffer().AsSpan().Slice(0, (int)memoryStream.Length), result);
                parser.Parse();
                var parsed = result.Span.Slice(0, parser.ResultIndex);
                Assert.AreEqual(1, parsed.Length);
                Assert.IsInstanceOfType(parsed[0], typeof(ArrayProtocolObject));
                var array1Items = (parsed[0] as ArrayProtocolObject).Items.Span;
                Assert.AreEqual("SET", (array1Items[0] as StringProtocolObject).Value);
                Assert.IsInstanceOfType(array1Items[1], typeof(MapProtocolObject));

                var map1Items = (array1Items[1] as MapProtocolObject).Items.Span;
                Assert.AreEqual("key1", (map1Items[0].Key as StringProtocolObject).Value);
                Assert.AreEqual("value1", (map1Items[0].Value as StringProtocolObject).Value);
                Assert.AreEqual("key2", (map1Items[1].Key as StringProtocolObject).Value);
                Assert.AreEqual("value2", (map1Items[1].Value as StringProtocolObject).Value);
                Assert.AreEqual("key3", (map1Items[2].Key as StringProtocolObject).Value);
                Assert.IsInstanceOfType(map1Items[2].Value, typeof(ArrayProtocolObject));
                var array2Items = (map1Items[2].Value as ArrayProtocolObject).Items.Span;
                Assert.AreEqual(true, (array2Items[0] as BooleanProtocolObject).Value);
                Assert.IsInstanceOfType(array2Items[1], typeof(NullProtocolObject));
            }
        }
    }
}
