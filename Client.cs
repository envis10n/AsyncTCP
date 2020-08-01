using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace AsyncTCP
{
    public class Client
    {
        public readonly Guid Id = Guid.NewGuid();
        public event Action OnConnect;
        public event Action OnDisconnect;
        public event Action<byte[]> OnData;
        public event Action<SocketException> OnError;
        private TcpClient _client;
        public CancellationToken cancellation = CancellationToken.None;
        public Encoding DefaultEncoding = Encoding.UTF8;
        public static Client Connect(string host, int port)
        {
            return new Client(new TcpClient(host, port));
        }
        public Client(TcpClient client)
        {
            _client = client;

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _client.NoDelay = true;

            Task t = Task.Run(() =>
            {
                while (!cancellation.IsCancellationRequested && _client.Connected)
                {
                    try
                    {
                        byte[] buffer = new byte[_client.ReceiveBufferSize];
                        int bytesRead = _client.Client.Receive(buffer);
                        if (bytesRead == 0) break;
                        if (bytesRead < buffer.Length)
                        {
                            byte[] temp = new byte[bytesRead];
                            Buffer.BlockCopy(buffer, 0, temp, 0, bytesRead);
                            buffer = temp;
                        }
                        InvokeOnData(buffer);
                    }
                    catch (SocketException e)
                    {
                        InvokeOnError(e);
                        break;
                    }
                }
                Close();
                InvokeOnDisconnect();
            });
            InvokeOnConnect();
        }
        public void Send(string data)
        {
            Send(data, DefaultEncoding);
        }
        public void Send(string data, Encoding encoding)
        {
            Send(encoding.GetBytes(data));
        }
        public void Send(byte[] buffer)
        {
            _client.Client.Send(buffer);
        }
        public void Close()
        {
            _client.Close();
        }
        private void InvokeOnConnect()
        {
            if (OnConnect != null) OnConnect.Invoke();
        }
        private void InvokeOnDisconnect()
        {
            if (OnDisconnect != null) OnDisconnect.Invoke();
        }
        private void InvokeOnData(byte[] buffer)
        {
            if (OnData != null) OnData.Invoke(buffer);
        }
        private void InvokeOnError(SocketException e)
        {
            if (OnError != null) OnError.Invoke(e);
        }
    }
}
