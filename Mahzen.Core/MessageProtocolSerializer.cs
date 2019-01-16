using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{    
    class MessageProtocolSerializer
    {
        public static async Task<Command[]> ReadStreamAsync(Stream stream)
        {
            Memory<byte> buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                Parse(buffer.Span.Slice(bytesRead));

            }

            return null;
        }

        private static void Parse(Span<byte> buffer)
        {
            var parser = new Parser(buffer);
            parser.Parse();
            
        }
    }
}