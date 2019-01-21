using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /* Single Command Request: <array>
     *     - Single Command Array:
     *         0: <command_keyword_string>
     *         1-n: parameters
     * Single Command Response: <mmp_type>
     *
     * Pipelining Commands: We can send multiple commands contiguously:  
     *     Multi Command Request: <single_command_request><single_command_request>         
     *     Multi Command Response: <array_of_mmp_types>
     *     
     * See Parser for the list of mmp types.
     */
    public static class CommandSerializer
    {
        public static async Task<List<Command>> DeserializeAsync(Stream stream)
        {
            Memory<byte> buffer = new byte[8192];
            int bytesRead;
            var protocolObjectBuffer = new MessageProtocolObject[1024];
            Memory<MessageProtocolObject> protocolObjects = protocolObjectBuffer;
            var parserIndex = 0;
            Memory<byte> remainingBuffer = new byte[0];
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                var (currentParserIndex, remeaningBufferLength) = ParseProtocolObjects(remainingBuffer.Span, buffer.Span.Slice(0, bytesRead),
                    protocolObjects.Slice(parserIndex),
                    () =>
                    {
                        Array.Resize(ref protocolObjectBuffer, protocolObjectBuffer.Length + 1024);
                        protocolObjects = protocolObjectBuffer;
                    });
                parserIndex += currentParserIndex;
                remainingBuffer = buffer.Slice(bytesRead - remeaningBufferLength, remeaningBufferLength);
            }
            return ParseCommands(protocolObjects.Slice(0, parserIndex));
        }

        private static (int currentParserIndex, int remeaningBufferLength) ParseProtocolObjects(Span<byte> previousRemainingBuffer, Span<byte> nextBuffer,
            Memory<MessageProtocolObject> result, Action resizeResult)
        {
            Span<byte> buffer = stackalloc byte[previousRemainingBuffer.Length + nextBuffer.Length];
            previousRemainingBuffer.CopyTo(buffer);
            nextBuffer.CopyTo(buffer.Slice(previousRemainingBuffer.Length));
            var parser = new Parser(buffer, result, resizeResult);
            parser.Parse();
            return (parser.ResultIndex, parser.RemeaningBuffer.Length);
        }

        private static List<Command> ParseCommands(Memory<MessageProtocolObject> protocolObjects)
        {
            var result = new List<Command>();
            var datas = protocolObjects.Span;
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] is ArrayProtocolObject singleCommandRequest)
                {
                    var items = singleCommandRequest.Items.Span;
                    if (items.Length < 1
                        || !(items[0] is StringProtocolObject keyword))
                    {
                        throw new SyntaxErrorException("Expecting command keyword string at first item of the array at index {0}", i);
                    }

                    result.Add(new Command
                    {
                        Keyword = keyword.Value,
                        Parameters = singleCommandRequest.Items.Slice(1)
                    });
                }
                else
                {
                    throw new SyntaxErrorException("Expecting array type for command request at index {0}", i);
                }
            }

            return result;
        }


    }
}