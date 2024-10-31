﻿using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;


namespace MessengerApp
{
    public partial class MainPage : ContentPage
    {
        private bool isConnected = false;
        private ObservableCollection<ChatMessage> messages = new ObservableCollection<ChatMessage>();
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private CancellationTokenSource receiveCts;
        private string clientID;

        public MainPage()
        {
            InitializeComponent();
            MessagesList.ItemsSource = messages; // привязка коллекции к списку сообщений
        }

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            isConnected = !isConnected;

            if (isConnected)
            {
                try
                {
                    tcpClient = new TcpClient();
                    await tcpClient.ConnectAsync("127.0.0.1", 12345);
                    networkStream = tcpClient.GetStream();
                    receiveCts = new CancellationTokenSource();

                    byte[] buffer = new byte[1024];
                    int bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), receiveCts.Token);
                    clientID = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    UpdateConnectionStatusLabel(clientID);

                    messages.Add(new ChatMessage
                    {
                        TimeStamp = DateTime.Now,
                        Message = $"Вы присоединились к чату как {clientID}.",
                        SenderGuid = "Система",
                    });

                    StartReceivingMessages(receiveCts.Token);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось подключиться к серверу: {ex.Message}", "OK");
                    isConnected = false;
                    UpdateConnectionStatusLabel();
                }
            }
            else
            {
                DisconnectFromServer();
            }
        }

        private async void StartReceivingMessages(CancellationToken token)
        {
            byte[] buffer = new byte[1024];
            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            messages.Add(ParseMessage(message));
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (isConnected)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            messages.Add(new ChatMessage
                            {
                                TimeStamp = DateTime.Now,
                                SenderGuid = "Система",
                                Message = "Потеряно соединение с сервером"
                            });
                        });
                    }
                    break;
                }
            }
        }

        private ChatMessage ParseMessage(string message)
        {
            var timeEndIndex = message.IndexOf(')');
            var guidStartIndex = message.IndexOf("[");
            var guidEndIndex = message.IndexOf("]");
            if (timeEndIndex != -1 && guidStartIndex != -1 && guidEndIndex != -1)
            {
                // Извлечение времени, GUID отправителя и текста
                string timeString = message.Substring(1, timeEndIndex - 1); // Время в формате HH:mm:ss
                string senderGuid = message.Substring(guidStartIndex + 1, guidEndIndex - guidStartIndex - 1);
                string text = message.Substring(guidEndIndex + 2); // Остальной текст сообщения

                // Попытка преобразования строки времени в DateTime
                if (DateTime.TryParse(timeString, out DateTime timestamp))
                {
                    return new ChatMessage
                    {
                        SenderGuid = senderGuid,
                        Message = text,
                        TimeStamp = timestamp
                    };
                }
            }
            return null;
        }

        private void DisconnectFromServer()
        {
            receiveCts?.Cancel();
            networkStream?.Close();
            tcpClient?.Close();
            UpdateConnectionStatusLabel();
            messages.Add(new ChatMessage
            {
                TimeStamp = DateTime.Now,
                Message = "Вы отключились от чата",
                SenderGuid = "Система"

            });
            isConnected = false;
        }

        private void UpdateConnectionStatusLabel()
        {
            ConnectionStatusLabel.FormattedText = new FormattedString
            {
                Spans = {
                            new Span { Text = "Состояние: ", TextColor = Colors.Black },
                            new Span { Text = "Отключен ", TextColor = Colors.Red },
                        }
            };
        }

        private void UpdateConnectionStatusLabel(string clientID)
        {
            ConnectionStatusLabel.FormattedText = new FormattedString
            {
                Spans = {
                            new Span { Text = "Состояние: ", TextColor = Colors.Black },
                            new Span { Text = "Подключен ", TextColor = Colors.Green },
                            new Span { Text = "(ID: " , TextColor = Colors.Black },
                            new Span { Text = $"{clientID}", TextColor= Colors.Blue },
                            new Span { Text = ")", TextColor = Colors.Black }
                        }
            };
        }

        private void SendMessageByPressingEnterOrSendButton()
        {
            if (!isConnected)
            {
                DisplayAlert("Ошибка", "Сначала подключитесь к чату", "ОК");
                return;
            }

            string messageText = MessageEntry.Text;
            if (!string.IsNullOrEmpty(messageText))
            {
                messages.Add(new ChatMessage
                {
                    TimeStamp = DateTime.Now,
                    Message = messageText,
                    SenderGuid = clientID,
                });

                MessageEntry.Text = "";
            }
        }

        private void SendButton_Clicked(object sender, EventArgs e)
        {
            SendMessageByPressingEnterOrSendButton();
        }

        private void OnMessageEntryCompleted(object sender, EventArgs e)
        {
            SendMessageByPressingEnterOrSendButton();
        }
    }

    public class ChatMessage
    {
        public required string Message { get; set; }
        public required string SenderGuid { get; set; }
        public required DateTime TimeStamp { get; set; }

        public override string ToString()
        {
            return $"({TimeStamp:HH:mm:ss}) [{SenderGuid}]: {Message}";
        }
    }
}
