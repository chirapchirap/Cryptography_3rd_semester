using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessengerHost
{
    public class Program
    {
        private TcpListener tcpListener;
        private List<ClientInfo> tcpClients = new List<ClientInfo>();

        public Program(string ipAddress, int port)
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Parse(ipAddress), port);
        }
        private void Main(string[] args)
        {
            tcpListener.Start();
            Console.WriteLine("Сервер запущен...");

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                NetworkStream stream = tcpClient.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string clientName = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Guid clientId = Guid.NewGuid();

                tcpClients.Add(new ClientInfo { client = tcpClient, id = clientId, name = clientName});
                Console.WriteLine($"Клиент {clientName} ({clientId}) подключен...");

                BroadcastClientsList(); // Отправка списка клиентов

                Thread clientThread = new Thread(() => HandleClient(tcpClient, clientId, clientName));
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient tcpClient, Guid clientId, string clientName)
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0) 
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Сообщение от клиента {clientName} ({clientId}): " + message);

                if (message.StartsWith("ОДНОМУ:"))
                {
                    var parts = message.Split(':');
                    string targetIdentifier = parts[1];
                    string privateMessage = parts[2];

                    if (Guid.TryParse(targetIdentifier, out Guid targetClientId))
                    {
                        // Отправка по GUID
                        SendToClient(privateMessage, targetClientId);
                    }
                    else
                    {
                        // Отправка по имени
                        SendToClientByName(privateMessage, targetIdentifier);
                    }
                }
                else
                {
                    BroadcastMessage(message, tcpClient);
                }
            }

            tcpClients.RemoveAll(c => c.client == tcpClient);
            tcpClient.Close();
            BroadcastClientsList();
        }

        private void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (var client in tcpClients)
            {
                if (client.client != sender)
                {
                    NetworkStream stream = client.client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        private void SendToClientByName(string message, string clientName)
        {
            var targetClient = tcpClients.FirstOrDefault(c => c.name == clientName);
            if (targetClient != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = targetClient.client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }

        private void SendToClient(string message, Guid clientId)
        {
            var targetClient = tcpClients.FirstOrDefault(c => c.id == clientId);
            if (targetClient != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = targetClient.client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }

        private void BroadcastClientsList()
        {
            string clientsList = "CLIENTS:" + string.Join(",", tcpClients.Select(c => $"{c.id},{c.name}"));
            BroadcastMessage(clientsList, null);
        }
    }

    public class ClientInfo
    {
        public TcpClient client { get; set; }
        public Guid id { get; set; }
        public string name { get; set; }
    }
}
