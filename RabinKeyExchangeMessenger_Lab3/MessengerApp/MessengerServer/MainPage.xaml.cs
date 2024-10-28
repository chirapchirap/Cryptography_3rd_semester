﻿using System.Collections.ObjectModel;
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
            server = new MessengerServer(ipAddress, port);

            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.MessageReceived += OnMessageReceived;

            await server.StartAsync();
            Logs.Add($"Сервер запущен на {ipAddress}:{5000}");
        }



        private void StopServerButton_Clicked(object sender, EventArgs e)
        {

        }
    }

}
