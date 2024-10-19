using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
                Guid clientId = Guid.NewGuid();
                tcpClients.Add(new ClientInfo { tcpClient = tcpClient, Id = clientId});
                Console.WriteLine($"Клиент {clientId} подключен...");

                BroadcastClientsList(); // Отправка списка клиентов
            }
        }

        private void BroadcastClientsList()
        {
            
        }
    }

    public class ClientInfo
    {
        public TcpClient tcpClient { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
