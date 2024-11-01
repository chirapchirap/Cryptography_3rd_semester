using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using ClassLib;


namespace MessengerApp
{
    public partial class MainPage : ContentPage
    {
        private bool isConnected = false;
        private readonly ObservableCollection<ClassLib.ChatMessage> messages = new ObservableCollection<ClassLib.ChatMessage>();
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
                    connectButton.IsEnabled = false;
                    disconnectButton.IsEnabled = true;
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
                        ClassLib.ChatMessage? message = System.Text.Json.JsonSerializer.Deserialize<ClassLib.ChatMessage>(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        if (message != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                messages.Add(message);
                            });
                        }
                    }
                    else
                    {
                        throw new Exception("Соединение с сервером потеряно");
                    }
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (isConnected)
                        {
                            messages.Add(new ClassLib.ChatMessage
                            {
                                TimeStamp = DateTime.Now,
                                SenderGuid = "Системное сообщение",
                                Message = ex.Message,
                            });
                            DisconnectFromServer();
                        }
                    });
                    break;
                }
            }
        }

        private void DisconnectFromServer()
        {
            receiveCts?.Cancel();
            networkStream?.Close();
            tcpClient?.Close();
            UpdateConnectionStatusLabel();
            messages.Add(new ClassLib.ChatMessage
            {
                TimeStamp = DateTime.Now,
                Message = "Вы отключены от чата",
                SenderGuid = "Системное сообщение"

            });
            isConnected = false;
            connectButton.IsEnabled = true; // Разблокируем кнопку подключения
            disconnectButton.IsEnabled = false; // Блокируем кнопку отключения
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

        private async void SendMessageByPressingEnterOrSendButton()
        {
            if (!isConnected)
            {
                await DisplayAlert("Ошибка", "Сначала подключитесь к чату", "ОК");
                return;
            }

            string messageText = MessageEntry.Text;

            if (!string.IsNullOrEmpty(messageText))
            {
                ClassLib.ChatMessage message = new ClassLib.ChatMessage
                {
                    TimeStamp = DateTime.Now,
                    Message = messageText,
                    SenderGuid = "Вы",
                };
                messages.Add(message);
                MessageEntry.Text = string.Empty;
                
                message.SenderGuid = clientID;
                byte[] messageBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize<ClassLib.ChatMessage>(message));
                await networkStream.WriteAsync(messageBytes.AsMemory(0, messageBytes.Length));
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

        private void DisconnectButton_Clicked(object sender, EventArgs e)
        {
            if (isConnected)
            {
                DisconnectFromServer();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (isConnected)
            {
                DisconnectFromServer();
            }
        }
    }
}
