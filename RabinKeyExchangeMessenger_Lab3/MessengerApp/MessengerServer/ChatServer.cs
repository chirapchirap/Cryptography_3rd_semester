using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    internal class ChatServer
    {
        private TcpListener server;
        private List<TcpClient> clients  = new List<TcpClient>();
        private bool isRunning;

        public ChatServer(string ip, int port)
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
        }
    }
}
