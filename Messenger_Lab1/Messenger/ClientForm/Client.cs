using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientForm
{
    public partial class Client : Form
    {
        private TcpClient client;
        private NetworkStream stream;

        public Client()
        {
            InitializeComponent();
            TryConnectToServer();
            textBoxMessage.KeyDown += TextBoxMessage_KeyDown; // Добавляем обработчик для Enter
        }

        // Метод для попытки подключения
        private async void TryConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 8080);  // Подключаемся к серверу
                stream = client.GetStream();  // Получаем поток данных
                richTextBox1.AppendText("Подключено к серверу.\n");
                await Task.Run(() => ReceiveMessages());  // Начинаем получать сообщения
            }
            catch (SocketException)  // Обрабатываем исключение при ошибке подключения
            {
                richTextBox1.Clear();
                await Task.Delay(50);
                richTextBox1.AppendText("Не удалось подключиться к серверу. Нажмите 'Переподключиться' для повторной попытки.\n");
            }
        }

        // Метод для получения сообщений
        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int byteCount;
            try
            {
                while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    string timestamp = DateTime.Now.ToString("HH:mm");
                    richTextBox1.Invoke((Action)(() => richTextBox1.AppendText($"[{timestamp}] Сервер: {message}\n")));
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

        // Метод отправки сообщения
        private async void ButtonSend_Click(object sender, EventArgs e)
        {
            if (client != null && stream != null)
            {
                string message = textBoxMessage.Text;

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;   
                }

                byte[] data = Encoding.UTF8.GetBytes(message);

                await stream.WriteAsync(data, 0, data.Length);
                string timestamp = DateTime.Now.ToString("HH:mm");
                richTextBox1.AppendText($"[{timestamp}] Вы (клиент): {message}\n");
                textBoxMessage.Clear();
            }
        }

        // Кнопка для повторной попытки подключения
        private void ButtonRefresh_Click(object sender, EventArgs e)
        {
            if (client != null)
            {
                stream?.Close();
                client?.Close();
            }
            richTextBox1.AppendText("Повторная попытка подключения...\n");
            TryConnectToServer();  // Повторно подключаемся
        }

        // Обработчик нажатия Enter в textBoxMessage
        private void TextBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Отключаем стандартное поведение клавиши Enter
                ButtonSend_Click(sender, e); // Вызываем отправку сообщения
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stream?.Close();
            client?.Close();
        }
    }
}
