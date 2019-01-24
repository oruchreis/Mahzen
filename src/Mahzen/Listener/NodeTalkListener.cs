using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mahzen.Core;
using Mahzen.Configuration;
using Serilog;

namespace Mahzen.Listener
{
    public class NodeTalkListener
    {
        private readonly CancellationToken _cancelToken;
        private readonly TcpListener _listener;

        private readonly ConcurrentDictionary<TcpClient, bool> _connectedNodes = new ConcurrentDictionary<TcpClient, bool>();

        public NodeTalkListener(CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            _listener = new TcpListener(IPAddress.Parse(Settings.Get.Node.ListenIpAddress), Settings.Get.Node.NodeTalkPort);
        }

        public async Task StartAsync()
        {
            await Task.Yield();

            _listener.Start();
            Log.Information("Node talk listener started at {0}", _listener.LocalEndpoint);

            //listener must be active until the cancel token is requested.
            while (!_cancelToken.IsCancellationRequested)
            {
                try
                {
                    var acceptTask = _listener.AcceptTcpClientAsync();
                    var timeoutTask = Task.Delay(3000, _cancelToken);
                    //waiting accept task for 3sn. 
                    await Task.WhenAny(timeoutTask, acceptTask);

                    if (!acceptTask.IsCompleted)
                        continue;

                    //get the client.
                    var client = await acceptTask;

#pragma warning disable 4014
                    HandleClientAsync(client);
#pragma warning restore 4014
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error occured when accepting a node.");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            await Task.Yield();

            //registering node
            _connectedNodes[tcpClient] = true;
            Log.Debug("Node is connected: {0}", tcpClient.Client.LocalEndPoint);

            try
            {
                var stream = tcpClient.GetStream();
                using (var dispatcher = new NodeTalkCommandDispatcher(stream, _cancelToken))
                {
                    //while node is connected, we listen the stream.
                    while (tcpClient.Connected && !_cancelToken.IsCancellationRequested)
                    {
                        //main message pipeline:
                        await dispatcher.HandleAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occured when handling node talk connection.");
            }
            finally
            {
                //unregistering client
                _connectedNodes[tcpClient] = false;
                _connectedNodes.TryRemove(tcpClient, out var _);
                Log.Debug("Node is disconnected: {0}", tcpClient.Client.LocalEndPoint);

                tcpClient.Dispose();//disposing client and the stream.
            }
        }
    }
}