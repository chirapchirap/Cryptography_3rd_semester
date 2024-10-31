using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using ClassLib;


namespace MessengerServer
{
    public partial class MainPage : ContentPage
    {
        private string ipAddress = "127.0.0.1";
        private int port = 12345;

        private MessengerServer? server;
        public ObservableCollection<ClassLib.ChatMessage> Logs { get; set; } = new ObservableCollection<ClassLib.ChatMessage>();
        public ObservableCollection<Guid> ConnectedClients { get; set; } = new ObservableCollection<Guid>();

        public MainPage()
        {
            InitializeComponent();
            LogsListView.ItemsSource = Logs;
            ClientsListView.ItemsSource = ConnectedClients;
        }

        private async void StartServerButton_Clicked(object sender, EventArgs e)
        {
            startServerButton.IsEnabled = false;
            server = new MessengerServer(ipAddress, port);

            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.MessageReceived += OnMessageReceived;
            server.ExceptionThrown += OnExceptionThrown;
            await server.StartAsync();
            Logs.Add(new ClassLib.ChatMessage
            {
                Message = $"Сервер запущен на {ipAddress}:{port}",
                TimeStamp = DateTime.Now,
                SenderGuid = "Системное сообщение"
            });
            StatusText.Text = "запущен";
            StatusText.TextColor = Colors.Green;
            stopServerButton.IsEnabled = true;
        }

        private void OnExceptionThrown(ClassLib.ChatMessage message)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                Logs.Add(message);
            });
        }

        private void OnMessageReceived(ClassLib.ChatMessage message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Logs.Add(message);
            });
        }

        private void OnClientDisconnected(Guid clientID)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectedClients.Remove(clientID);
                Logs.Add(new ClassLib.ChatMessage
                {
                    Message = "Клиент отключен",
                    SenderGuid = clientID.ToString(),
                    TimeStamp = DateTime.Now
                });
            });
        }

        private void OnClientConnected(Guid clientID)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectedClients.Add(clientID);
                Logs.Add(new ClassLib.ChatMessage
                {
                    Message = "Клиент подключен",
                    SenderGuid = clientID.ToString(),
                    TimeStamp = DateTime.Now
                });
            });
        }

        private void StopServerButton_Clicked(object sender, EventArgs e)
        {
            if (server != null)
            {
                stopServerButton.IsEnabled = false;
                server.Stop();

                server.ClientConnected -= OnClientConnected;
                server.ClientDisconnected -= OnClientDisconnected;
                server.MessageReceived -= OnMessageReceived;
                server = null;
                ConnectedClients.Clear();
                Logs.Add(new ClassLib.ChatMessage
                {
                    Message = "Сервер остановлен",
                    SenderGuid = "Системное сообщение",
                    TimeStamp = DateTime.Now
                });
                startServerButton.IsEnabled = true;
                StatusText.Text = "остановлен";
                StatusText.TextColor = Colors.Red;
            }
        }
    }

}
