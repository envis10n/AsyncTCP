using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Envis10n.AsyncTCP
{
    using Lib;
    public class Server : IDisposable
    {
        public event Action OnListen;
        public event Action<Client> OnConnect;
        public event Action<Client> OnDisconnect;
        public event Action<Client, byte[]> OnData;
        public event Action<SocketException> OnError;
        private TcpListener _listener;

        public readonly IPEndPoint BindEndpoint;
        public readonly IPAddress BindAddress;
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        public readonly int Port;
        public readonly Thread Listener;
        private ConcurrentDictionary<Guid, Client> Clients = new ConcurrentDictionary<Guid, Client>();
        public Server(int port, string host = "127.0.0.1")
        {
            Port = port;
            BindAddress = Dns.GetHostAddresses(host)[0];
            BindEndpoint = new IPEndPoint(BindAddress, port);
            _listener = new TcpListener(BindEndpoint);
            Listener = new Thread(ListenLoop);
            Listener.Start();
        }
        private void ListenLoop()
        {
            _listener.Start();
            InvokeOnListen();
            Task<TcpClient> acceptTask = _listener.AcceptTcpClientAsync();
            while (!cancellation.IsCancellationRequested)
            {
                if (acceptTask.IsCompleted)
                {
                    TcpClient sock = acceptTask.Result;
                    acceptTask = _listener.AcceptTcpClientAsync();
                    Task.Run(() =>
                    {
                        try
                        {
                            Client client = new Client(sock);
                            client.cancellation = cancellation.Token;
                            using (var loc = Clients.Lock())
                            {
                                loc.Value.Add(client.Id, client);
                            }
                            client.OnData += (buffer) =>
                            {
                                InvokeOnData(client, buffer);
                            };
                            client.OnDisconnect += () =>
                            {
                                InvokeOnDisconnect(client);
                                using (var loc = Clients.Lock())
                                {
                                    loc.Value.Remove(client.Id);
                                }
                            };
                            InvokeOnConnect(client);
                        }
                        catch (SocketException e)
                        {
                            InvokeOnError(e);
                        }
                    }, cancellation.Token);
                }
            }
            _listener.Stop();
        }
        private void InvokeOnListen()
        {
            if (OnListen != null) OnListen.Invoke();
        }
        private void InvokeOnConnect(Client client)
        {
            if (OnConnect != null) OnConnect.Invoke(client);
        }
        private void InvokeOnDisconnect(Client client)
        {
            if (OnDisconnect != null) OnDisconnect.Invoke(client);
        }
        private void InvokeOnData(Client client, byte[] buffer)
        {
            if (OnData != null) OnData.Invoke(client, buffer);
        }
        private void InvokeOnError(SocketException e)
        {
            if (OnError != null) OnError.Invoke(e);
        }
        public void Close()
        {
            using (var loc = Clients.Lock())
            {
                foreach (Client client in loc.Value.Values)
                {
                    client.Close();
                }
            }
            cancellation.Cancel();
        }
        public void Dispose()
        {
            Close();
            // Wait for the thread to shutdown before continuing.
            Listener.Join(10000);
        }
    }
}