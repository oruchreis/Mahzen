using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mahzen.Core;
using Mahzen.Configuration;
using Serilog;

namespace Mahzen.Listener
{
    public class NodeListener
    {
        private readonly CancellationToken _cancelToken;
        private readonly TcpListener _listener; 
        
        private readonly ConcurrentDictionary<TcpClient, bool> _connectedClients = new ConcurrentDictionary<TcpClient, bool>();
            
        public NodeListener(CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            _listener = new TcpListener(IPAddress.Parse(Settings.Get.Node.ListenIpAddress), Settings.Get.Node.Port);            
        }

        public async Task StartAsync()
        {
            await Task.Yield();
            
            _listener.Start();
            Log.Information("Listening socket {0}", _listener.LocalEndpoint);
            
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
                    Log.Error(e, "Error occured when accepting client.");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            await Task.Yield();
            
            //registering client
            _connectedClients[tcpClient] = true;            
            Log.Debug("Client is connected: {0}", tcpClient.Client.LocalEndPoint);
            
            try
            {
                var stream = tcpClient.GetStream();
                var dispatcher = new CommandDispatcher(stream, _cancelToken);
                //while client is connected, we listen the stream.
                while (tcpClient.Connected && !_cancelToken.IsCancellationRequested)
                {
                    //main message pipeline:
                    await dispatcher.HandleAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error occured when handling connection.");                
            }
            finally
            {
                //unregistering client
                _connectedClients[tcpClient] = false;
                _connectedClients.TryRemove(tcpClient, out var _);
                Log.Debug("Client is disconnected: {0}", tcpClient.Client.LocalEndPoint);
                
                tcpClient.Dispose();//disposing client and the stream.
            }
        }
    }
}