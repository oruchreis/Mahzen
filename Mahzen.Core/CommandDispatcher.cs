using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    public class CommandDispatcher
    {
        private readonly NetworkStream _stream;
        private readonly CancellationToken _cancelToken;

        public CommandDispatcher(NetworkStream stream, CancellationToken cancelToken)
        {
            _stream = stream;
            _cancelToken = cancelToken;
        }

        public async Task HandleAsync()
        {
            var requests = await MessageProtocolSerializer.ReadStreamAsync(_stream);
        }
    }
}