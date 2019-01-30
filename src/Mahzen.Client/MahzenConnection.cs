using Mahzen.Core;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Client
{
    public sealed class MahzenConnection
    {
        public static async Task<MahzenConnection> ConnectAsync(string connectionString)
        {
            var connection = new MahzenConnection(connectionString);

            await connection.OpenOneConnectionAsync().ConfigureAwait(false);

            return connection;
        }


        public Nodes Nodes { get; }

        private MahzenConnection(string connectionString)
        {
            Nodes = new Nodes(this);

            var parts = connectionString.Split(';');

            //todo: parse options after index 0

            foreach (var endpoint in parts[0].Split(','))
            {
                if (IPEndPoint.TryParse(endpoint, out var ipEndpoint))
                {
                    Nodes.Bucket[endpoint] = new Node(ipEndpoint);
                }
                else
                    throw new ArgumentException($"Invalid endpoint format '{endpoint}'. Valid format is Ip:Port", nameof(connectionString));
            }
        }

        private async Task OpenOneConnectionAsync()
        {
            await Task.WhenAny(Nodes.Select(node => node.ConnectAsync())).ConfigureAwait(false);
        }
    }

    public sealed class Nodes : IEnumerable<Node>
    {
        private readonly MahzenConnection _connection;

        internal Nodes(MahzenConnection connection)
        {
            _connection = connection;
        }

        internal readonly ConcurrentDictionary<string, Node> Bucket = new ConcurrentDictionary<string, Node>();

        public Node this[string nodeSocket]
        {
            get => Bucket.TryGetValue(nodeSocket, out var node) ? node : null;
        }

        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        {
            foreach (var kv in Bucket)
            {
                yield return kv.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Node>)this).GetEnumerator();
        }
    }

    public sealed class Node : IDisposable
    {
        internal Node(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
        }

        public IPEndPoint Endpoint { get; }

        private readonly TcpClient _tcpClient = new TcpClient();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Node() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion        

        internal async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(Endpoint.Address, Endpoint.Port).ConfigureAwait(false);
        }

        private MessageProtocolWriter GetCommandWriter(Command command)
        {
            var writer = new MessageProtocolWriter(_tcpClient.GetStream());
            writer.Write(command);
            return writer;
        }

        public bool Ping()
        {
            using(var writer = GetCommandWriter(new Command("PING")))
                writer.Flush();
            
        }

        public async Task<bool> PingAsync()
        {
            using (var writer = GetCommandWriter(new Command("PING")))
                await writer.FlushAsync().ConfigureAwait(false);
        }
    }
}
