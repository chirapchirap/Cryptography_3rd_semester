using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    internal class ChatServer
    {
        private TcpListener server;
        private List<TcpClient> clients  = new List<TcpClient>();
        private bool isRunning;

        public ChatServer(string ip, int port)
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
        }

        public void StartServer()
        {
            server.Start();
            isRunning = true;
            Task.Run(() => AcceptClientsAsync());
        }

        private async void AcceptClientsAsync()
        {
            while (isRunning)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                clients.Add(client);
                Task.Run(() => HandleClientsAsync(client));
            }
        }

        private async void HandleClientsAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();  
            byte[] buffer = new byte[4096];

            while (isRunning)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount > 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                BroadcastMessage(message, client);
            }

            clients.Remove(client);
            client.Close();
        }

        private void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients)
            {
                if (client == sender) continue;
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        public void StopServer()
        {
            isRunning = false;
            server.Stop();
            foreach (var client in clients)
            {
                client.Close();
            }
            clients.Clear();
        }
    }
}
