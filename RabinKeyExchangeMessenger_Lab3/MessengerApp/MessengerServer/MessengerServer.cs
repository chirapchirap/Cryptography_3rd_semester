using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    internal class MessengerServer
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<Guid, TcpClient> clients = new ConcurrentDictionary<Guid, TcpClient>();

        public event Action<Guid> ClientConnected;
        public event Action<Guid> ClientDisconnected;
        public event Action<Guid, string> MessageReceived;
        public event Action<Guid, string, Exception> ExceptionThrown;

        public MessengerServer(string ip, int port)
        {
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public async Task StartAsync()
        {
            tcpListener.Start();

            while (true)
            {
                var client = await tcpListener.AcceptTcpClientAsync();
                var clientID = Guid.NewGuid();
                
                clients.TryAdd(clientID, client);

                ClientConnected?.Invoke(clientID);

                _ = Task.Run(() => HandleClientAsync(client, clientID));
            }
        }

        private async void HandleClientAsync(TcpClient client, Guid clientID)
        {
            try
            {
                using (var stream = client.GetStream()) 
                {
                    // Отправка GUID клиенту
                    string welcomeMessage = $"Ваш уникальный идентификатор: {clientID}";
                    byte[] welcomeData = Encoding.UTF8.GetBytes(welcomeMessage);
                    await stream.WriteAsync(welcomeData, 0, welcomeData.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    
                    // Чтение данных от клиента
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    { 
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Вызов события получения сообщения 
                        MessageReceived?.Invoke(clientID, message);

                        // Рассылка сообщения всем клиентам
                        await BroadcastMessageAsync(clientID, message);
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {

            }
        }
    }
}
