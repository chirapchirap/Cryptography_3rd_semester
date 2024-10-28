using System.Collections.ObjectModel;
using System.Net.Sockets;


namespace MessengerApp
{
    public partial class MainPage : ContentPage
    {
        private bool isConnected = false;
        private ObservableCollection<ChatMessage> messages = new ObservableCollection<ChatMessage>();
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private string clientID;

        public MainPage()
        {
            InitializeComponent();
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
                    clientID = Guid.NewGuid().ToString();
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

                    messages.Add(new ChatMessage
                    {
                        Message = "Вы подключены к чату.",
                        SenderGuid = clientID,
                        TimeStamp = DateTime.Now,
                    });

                }
                catch (Exception ex) 
                {
                    await DisplayAlert("Ошибка", $"Не удалось подключиться к серверу: {ex.Message}", "OK");
                    isConnected = false;
                    StatusText.Text = "отключен";
                    StatusText.TextColor = Colors.Red;
                }
            }
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
                    Message = messageText,
                    SenderGuid = clientID,
                    TimeStamp = DateTime.Now,
                });

                MessageEntry.Text = "";
            }
        }

        private void SendButton_Clicked(object sender, EventArgs e)
        {
            SendMessageByPressingEnterOrSendButton();
        }
    }



    public class ChatMessage
    {
        public string Message { get; set; }
        public string SenderGuid { get; set; }

        public DateTime TimeStamp { get; set; }

        public override string ToString()
        {
            return $"({TimeStamp:HH:mm:ss}) [{SenderGuid}]: {Message}";
        }
    }
}
