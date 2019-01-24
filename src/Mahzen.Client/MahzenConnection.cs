using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mahzen.Client
{
    public class MahzenConnection
    {
        public static async Task<MahzenConnection> ConnectAsync(string connectionString)
        {
            var connection = new MahzenConnection(connectionString);

            await connection.OpenOneConnectionAsync().ConfigureAwait(false);

            return connection;
        }

        private readonly ConcurrentDictionary<string, Node> _nodes = new ConcurrentDictionary<string, Node>();

        private MahzenConnection(string connectionString)
        {
            var parts = connectionString.Split(';');
            var nodeEndpoints = parts[0].Split(',');

            //todo: parse options after index 0

            foreach (var endpoint in nodeEndpoints)
            {
                if (IPEndPoint.TryParse(endpoint, out var ipEndpoint))
                {
                    _nodes[endpoint] = new Node(ipEndpoint);
                }
                else
                    throw new ArgumentException($"Invalid endpoint format '{endpoint}'. Valid format is Ip:Port", nameof(connectionString));
            }
        }

        private async Task OpenOneConnectionAsync()
        {
            await Task.WhenAny(_nodes.Values.Select(async node =>
            {
                node.TcpClient = new TcpClient
                {

                };
                //todo: handle timeout
                await node.TcpClient.ConnectAsync(node.Endpoint.Address, node.Endpoint.Port).ConfigureAwait(false);
            }));
        }
    }

    internal class Node
    {
        public Node(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
        }
        public IPEndPoint Endpoint { get; private set; }

        private TcpClient _tcpClient;
        private readonly ReaderWriterLockSlim _tcpClientLocker = new ReaderWriterLockSlim();

        public TcpClient TcpClient
        {
            get
            {
                _tcpClientLocker.EnterReadLock();
                try
                {
                    return _tcpClient;
                }
                finally
                {
                    _tcpClientLocker.ExitWriteLock();
                }
            }
            set
            {
                _tcpClientLocker.EnterWriteLock();
                try
                {
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                    _tcpClient = value;
                }
                finally
                {
                    _tcpClientLocker.ExitWriteLock();
                }
            }
        }
    }
}
