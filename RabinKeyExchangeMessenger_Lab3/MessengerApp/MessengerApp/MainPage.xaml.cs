using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ClassLib;
using static System.Runtime.InteropServices.JavaScript.JSType;


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
        private RabinCryptoSystem cryptoSystem;

        // Словарь для хранения GUID и публичных ключей других клиентов
        private ConcurrentDictionary<Guid, BigInteger> clientPublicKeys = new ConcurrentDictionary<Guid, BigInteger>();

        public MainPage()
        {
            InitializeComponent();
            MessagesList.ItemsSource = messages; // привязка коллекции к списку сообщений
            cryptoSystem = new RabinCryptoSystem();  // Инициализация криптосистемы
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

                    // Отправка публичного ключа при подключении
                    byte[] publicKeyBytes = cryptoSystem.N.ToByteArray();
                    await networkStream.WriteAsync(publicKeyBytes.AsMemory(0, publicKeyBytes.Length));

                    // Получаем и обновляем список публичных ключей всех клиентов
                    bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), receiveCts.Token);
                    string publicKeyListJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (publicKeyListJson.StartsWith("UpdatePublicKeys"))
                    {
                        UpdatePublicKeys(publicKeyListJson.Substring("UpdatePublicKeys".Length)); // Обновляем список публичных ключей
                    }
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
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Проверяем тип сообщения (например, "UpdatePublicKeys")
                        if (receivedData.StartsWith("UpdatePublicKeys"))
                        {
                            UpdatePublicKeys(receivedData.Substring("UpdatePublicKeys".Length));
                        }
                        else
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

        private void UpdatePublicKeys(string data)
        {
            clientPublicKeys.Clear(); // Очищаем старый список

            // Десериализуем полученные данные, содержащие GUID клиентов и их публичные ключи
            var keyData = System.Text.Json.JsonSerializer.Deserialize<ConcurrentDictionary<Guid, byte[]>>(data);

            foreach (var entry in keyData)
            {
                BigInteger publicKey = new BigInteger(entry.Value); // Парсим публичный ключ
                clientPublicKeys[entry.Key] = publicKey; // Обновляем словарь
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
