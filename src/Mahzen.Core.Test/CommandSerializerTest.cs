using Mahzen.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core.Test
{
    [TestClass]
    public class CommandSerializerTest
    {
        [TestMethod]
        public async Task DeserializeTest()
        {
            using (var memStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memStream))
            {
                binaryWriter.Write(new ArrayProtocolObject
                {
                    Items = new MessageProtocolObject[]
                    {
                        new StringProtocolObject{Value="SET"},
                        new StringProtocolObject{Value="Test:Key:SubKey"},
                        new BlobProtocolObject{Bytes=Guid.NewGuid().ToByteArray()}
                    }
                });

                memStream.Position = 0;
                var commands = await CommandSerializer.DeserializeAsync(memStream);

                Assert.AreEqual(1, commands.Count);
                Assert.AreEqual("SET", commands[0].Keyword);
                Assert.AreEqual(2, commands[0].Parameters.Length);
                var parameters = commands[0].Parameters.ToArray();
                Assert.IsInstanceOfType(parameters[0], typeof(StringProtocolObject));
                var param1String = parameters[0] as StringProtocolObject;
                Assert.AreEqual("Test:Key:SubKey", param1String.Value);
                Assert.IsInstanceOfType(parameters[1], typeof(BlobProtocolObject));
            }
        }
    }
}
