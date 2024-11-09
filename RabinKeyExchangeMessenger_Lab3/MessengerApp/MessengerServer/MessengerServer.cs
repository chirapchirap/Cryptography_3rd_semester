using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassLib;

namespace MessengerServer
{
    internal class MessengerServer
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<Guid, TcpClient> clients = new ConcurrentDictionary<Guid, TcpClient>();
        private readonly ConcurrentDictionary<Guid, string> publicKeys = new ConcurrentDictionary<Guid, string>();
        public event Action<Guid> ClientConnected;
        public event Action<Guid> ClientDisconnected;
        public event Action<ClassLib.ChatMessage> MessageReceived;
        public event Action<ClassLib.ChatMessage> ExceptionThrown;

        private bool isRunning = false;
        public MessengerServer(string ip, int port)
        {
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public Task StartAsync()
        {
            isRunning = true;
            tcpListener.Start();
            Task.Run(async () =>
            {
                while (isRunning)
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    var clientID = Guid.NewGuid();

                    clients.TryAdd(clientID, client);

                    ClientConnected?.Invoke(clientID);

                    _ = Task.Run(() => HandleClientAsync(client, clientID));
                }
            });

            return Task.CompletedTask;
        }

        private async void HandleClientAsync(TcpClient client, Guid clientID)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    // Отправка GUID клиенту
                    byte[] assignedClientID = Encoding.UTF8.GetBytes(clientID.ToString());
                    await stream.WriteAsync(assignedClientID, 0, assignedClientID.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Чтение открытого ключа клиента
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string publicKey = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    publicKeys[clientID] = publicKey;
                    // Отправка новому клиенту списка идентификаторов всех уже подключённых клиентов
                    await SendConnectedClientsList(clientID);

                    // Оповещение всех клиентов о подключении нового клиента
                    NotifyClientsOfNewClient(clientID);

                    // Чтение данных от клиента
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        // Проверка, является ли запросом на открытый ключ
                        if (Guid.TryParse(request, out Guid requestedClientID) && publicKeys.ContainsKey(requestedClientID))
                        {
                            // Отправляем запрашиваемый открытый ключ клиенту
                            string requestedPublicKey = publicKeys[requestedClientID];
                            byte[] keyData = Encoding.UTF8.GetBytes(requestedPublicKey);
                            await stream.WriteAsync(keyData, 0, keyData.Length);
                        }
                        else
                        {
                            ClassLib.ChatMessage? chatMessage = System.Text.Json.JsonSerializer.Deserialize<ClassLib.ChatMessage>(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                            if (chatMessage != null)
                            {
                                // Вызов события получения сообщения 
                                MessageReceived?.Invoke(chatMessage);

                                // Рассылка сообщения всем клиентам
                                await BroadcastMessageAsync(chatMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ClassLib.ChatMessage? exceptionMessage = new ClassLib.ChatMessage
                {
                    Message = $"{"Ошибка на стороне клиента"} (Ошибка: {ex.Message})",
                    SenderGuid = clientID.ToString(),
                    TimeStamp = DateTime.Now,
                };
                ExceptionThrown?.Invoke(exceptionMessage);
            }
            finally
            {
                // Удаление клиента и закрытие соединения 
                ClientDisconnected?.Invoke(clientID);
                clients.TryRemove(clientID, out _);
                publicKeys.TryRemove(clientID, out _);
                client.Close();
            }
        }

        private async Task BroadcastMessageAsync(ClassLib.ChatMessage message)
        {
            byte[] data = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            foreach (var kvp in clients)
            {
                var clientID = kvp.Key;
                var client = kvp.Value;

                if (clientID != Guid.Parse(message.SenderGuid))
                {
                    try
                    {
                        var stream = client.GetStream();
                        await stream.WriteAsync(data, 0, data.Length);

                    }
                    catch (Exception ex)
                    {
                        ClassLib.ChatMessage? exceptionMessage = new ClassLib.ChatMessage
                        {
                            Message = $"{"Ошибка отправки сообщения клиенту"} (Ошибка: {ex.Message})",
                            SenderGuid = clientID.ToString(),
                            TimeStamp = DateTime.Now,
                        };
                        ExceptionThrown?.Invoke(exceptionMessage);
                    }
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            foreach (var client in clients)
            {
                client.Value.Close();
            }
            clients.Clear();
            tcpListener.Stop();
        }
    }
}
