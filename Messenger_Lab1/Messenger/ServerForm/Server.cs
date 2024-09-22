using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerForm
{
    public partial class Server : Form
    {
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;

        public Server()
        {
            InitializeComponent();
            StartServer();
        }

        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();
            richTextBox1.AppendText("Сервер запущен... Ожидание соединений.\n");

            client = await listener.AcceptTcpClientAsync();
            stream = client.GetStream();
            Task.Run(() => ReceiveMessages());

            richTextBox1.AppendText("Клиент подключён.\n");
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int byteCount;
            try
            {
                while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    richTextBox1.Invoke((Action)(() => richTextBox1.AppendText($"Клиент: {message}\n")));
                }
            }
            catch (Exception)
            {
                // Оповещение о том, что клиент или сервер отключены
                richTextBox1.Invoke((Action)(() =>
                {
                    richTextBox1.AppendText("Клиент отключен от сервера.\n");
                }));
            }
        }

        private async void buttonSend_Click(object sender, EventArgs e)
        {
            if (client != null && stream != null)
            {
                string message = textBoxMessage.Text;
                byte[] data = Encoding.UTF8.GetBytes(message);

                await stream.WriteAsync(data, 0, data.Length);
                richTextBox1.AppendText($"Вы (сервер): {message}\n");
                textBoxMessage.Clear();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stream?.Close();
            client?.Close();
            listener.Stop();
        }
    }
}
