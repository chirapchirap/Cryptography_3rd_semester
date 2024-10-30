﻿using System;
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
                string message = "Ошибка клиента";
                ExceptionThrown?.Invoke(clientID, message, ex);
            }
            finally
            {
                // Удаление клиента и закрытие соединения 
                ClientDisconnected?.Invoke(clientID);
                clients.TryRemove(clientID, out _);
                client.Close();
            }
        }

        private async Task BroadcastMessageAsync(Guid senderID, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes($"({DateTime.Now:HH:mm:ss}) {senderID}: {message}");

            foreach (var kvp in clients)
            {
                var clientID = kvp.Key;
                var client = kvp.Value;

                if (clientID != senderID)
                {
                    try
                    {
                        using (var stream = client.GetStream())
                        {
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        string logMessage = "Ошибка отправки сообщения клиенту";
                        ExceptionThrown?.Invoke(clientID, logMessage, ex);
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
