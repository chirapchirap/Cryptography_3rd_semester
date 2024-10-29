using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

namespace MessengerServer
{
    public partial class MainPage : ContentPage
    {
        private string ipAddress = "127.0.0.1";
        private int port = 5000;

        private MessengerServer server;
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
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
            Logs.Add($"({DateTime.Now:HH:mm:ss}) Сервер запущен на {ipAddress}:{5000}");
            StatusText.Text = "запущен";
            StatusText.TextColor = Colors.Red;
            stopServerButton.IsEnabled = true;
        }

        private void OnExceptionThrown(Guid clientID, string logMessage, Exception ex)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                Logs.Add($"({DateTime.Now:HH:mm:ss}) {logMessage} {clientID}: {ex.Message}");
            });
        }

        private void OnMessageReceived(Guid clientID, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Logs.Add($"({DateTime.Now:HH:mm:ss}) от {clientID}: {message}");
            });
        }

        private void OnClientDisconnected(Guid clientID)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectedClients.Remove(clientID);
                Logs.Add($"({DateTime.Now:HH:mm:ss}) Клиент {clientID} отключен.");
            });
        }

        private void OnClientConnected(Guid clientID)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectedClients.Add(clientID);
                Logs.Add($"({DateTime.Now:HH:mm:ss}) Клиент {clientID} подключен.");
            });
        }

        private void StopServerButton_Clicked(object sender, EventArgs e)
        {
            if (server != null)
            {
                stopServerButton.IsEnabled=false;
                server.Stop();
                Logs.Add($"({DateTime.Now:HH:mm:ss}) Сервер остановлен");
                StatusText.Text = "запущен";
                StatusText.TextColor = Colors.Red;

                server.ClientConnected -= OnClientConnected;
                server.ClientDisconnected -= OnClientDisconnected;
                server.MessageReceived -= OnMessageReceived;
                server = null;
                ConnectedClients.Clear();
                startServerButton.IsEnabled=true;
            }
        }
    }

}
