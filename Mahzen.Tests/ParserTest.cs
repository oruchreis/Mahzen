using System;
using System.Buffers.Text;
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
            var expectedLong = new Random().Next() + int.MaxValue;
            var expectedDouble = new Random().NextDouble();
            
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
            
            Assert.AreEqual(10, result.Count);
            var parsedBlob = result[0] as BlobProtocolData; 
            Assert.IsNotNull(parsedBlob);
            Assert.AreEqual(TokenType.Blob, parsedBlob.TokenType);
            CollectionAssert.AreEqual(expectedBlob, parsedBlob.Bytes);
            Assert.AreEqual(0, parser.RemeaningBuffer.Length);
        }
    }
}
