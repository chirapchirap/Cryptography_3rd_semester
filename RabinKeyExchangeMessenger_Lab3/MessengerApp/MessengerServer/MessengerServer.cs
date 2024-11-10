using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassLib;

namespace MessengerServer
{
    internal class MessengerServer
    {
        private readonly TcpListener tcpListener;
        private readonly ConcurrentDictionary<Guid, TcpClient> clients = new ConcurrentDictionary<Guid, TcpClient>();
        private readonly ConcurrentDictionary<Guid, BigInteger> publicKeys = new ConcurrentDictionary<Guid, BigInteger>();
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

        // Метод для отправки списка всех открытых ключей (GUID и открытых ключей) всем клиентам
        private async Task SendPublicKeysListToAllClientsAsync()
        {
            // Преобразуем список подключенных клиентов в формат, пригодный для отправки
            var clientsList = publicKeys.Select(client => new { client.Key, client.Value }).ToList();

            // Сериализация списка в формат JSON
            var json = JsonSerializer.Serialize(clientsList);
            byte[] messageData = Encoding.UTF8.GetBytes(json);

            // Отправляем список всем клиентам
            foreach (var client in clients.Values)
            {
                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(messageData, 0, messageData.Length);
                }
                catch (Exception ex)
                {
                    ClassLib.ChatMessage exceptionMessage = new ClassLib.ChatMessage
                    {
                        Message = $"Ошибка отправки списка ключей клиенту. (Ошибка: {ex.Message})",
                        SenderGuid = "Сервер",
                        TimeStamp = DateTime.Now,
                    };
                    ExceptionThrown?.Invoke(exceptionMessage);
                }
            }
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
                    byte[] publicKey = new byte[bytesRead];
                    Array.Copy(buffer, publicKey, bytesRead);                    
                    publicKeys[clientID] = new BigInteger(publicKey);

                    // Отправка списка всех подключенных клиентов (ключи и GUID)
                    await SendPublicKeysListToAllClientsAsync();

                    // Чтение данных от клиента
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        // Проверка, является ли запросом на открытый ключ
                        if (Guid.TryParse(request, out Guid requestedClientID) && publicKeys.ContainsKey(requestedClientID))
                        {
                            // Отправляем запрашиваемый открытый ключ клиенту
                            BigInteger requestedPublicKey = publicKeys[requestedClientID];
                            byte[] keyData = requestedPublicKey.ToByteArray();
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

                // После того как клиент отключится, отправляем обновленный список ключей всем остальным клиентам
                await SendPublicKeysListToAllClientsAsync();
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
