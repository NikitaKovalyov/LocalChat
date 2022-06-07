using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using playfairСipher;

namespace Client
{
    /* This Client. */
    
    public partial class Client : Form
    {
        private bool connected = false;
        private Thread client = null;
        private struct MyClient
        {
            public string username;
            public string key;
            public TcpClient client;
            public NetworkStream stream;
            public byte[] buffer;
            public StringBuilder data;
            public EventWaitHandle handle;
        };
        private MyClient obj;
        private Task send = null;
        private bool exit = false;
        public SecurityAlgorithm _target;

        public Client()
        {
            InitializeComponent(); // инициализация компонентов.
        }

        /* Логирование, если параметр не передается, то лог очищается. */
        private void Log(string msg = "")
        {
            if (!exit)
            {
                logTextBox.Invoke((MethodInvoker)delegate
                {
                    if (msg.Length > 0)
                    {
                        if (msg.Contains(":")) // сообщение от другого пользователя пользователю
                        {
                            string[] tmp = msg.Split(':'); // сплитим на массив строк
                            string key = tmp[1].Trim(); // выбираем 2 часть, там где ключ

                            if ((key[0] == '0'))
                            {
                                encryptionKeyTextBox.Text = key.Substring(1);
                            }
                            else
                            {
                                logTextBox.AppendText(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"), 
                                    msg, Environment.NewLine));

                                decryptButton.Enabled = true;
                            }
                        }
                        else // иначе если сообщение от сервера
                        {
                            if ((msg[0] == '0'))
                            {
                                encryptionKeyTextBox.Text = msg.Substring(1);
                            }
                            else
                            {
                                logTextBox.AppendText(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"), 
                                    msg, Environment.NewLine));
                            }
                        }
                    }
                    else
                    {
                        logTextBox.Clear();
                    }
                });
            }
        }

        /* Возвращает сообщение ошибки. */
        private string ErrorMsg(string msg)
        {
            return string.Format("ERROR: {0}", msg);
        }

        /* Возвращает системное сообщение. */
        private string SystemMsg(string msg)
        {
            return string.Format("SYSTEM: {0}", msg);
        }

        /* Активация кнопок. */
        private void Connected(bool status)
        {
            if (!exit)
            {
                connectButton.Invoke((MethodInvoker)delegate
                {
                    connected = status;
                    if (status)
                    {
                        addrTextBox.Enabled = false;
                        portTextBox.Enabled = false;
                        usernameTextBox.Enabled = false;
                        keyTextBox.Enabled = false;
                        connectButton.Text = "Disconnect";
                        Log(SystemMsg("You are now connected"));
                    }
                    else
                    {
                        addrTextBox.Enabled = true;
                        portTextBox.Enabled = true;
                        usernameTextBox.Enabled = true;
                        keyTextBox.Enabled = true;
                        connectButton.Text = "Connect";
                        Log(SystemMsg("You are now disconnected"));
                    }
                });
            }
        }

        /* Прослушивание клиентов. */
        public string lastString = "";
        public string tmp = "";
        private void Read(IAsyncResult result)
        {
            int bytes = 0;
            if (obj.client.Connected)
            {
                try
                {
                    bytes = obj.stream.EndRead(result);
                }
                catch (Exception ex)
                {
                    Log(ErrorMsg(ex.Message));
                }
            }
            if (bytes > 0)
            {
                obj.data.AppendFormat("{0}", Encoding.UTF8.GetString(obj.buffer, 0, bytes));

                try
                {
                    if (obj.stream.DataAvailable)
                    {
                        obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(Read), null);
                    }
                    else
                    {
                        Log(obj.data.ToString());

                        tmp = Encoding.UTF8.GetString(obj.buffer, 0, bytes);
                        //lastString = lastString.Substring(1);

                        if (tmp.Contains(":"))
                        {
                            string[] subs = tmp.Split(':');
                            lastString = subs[1];
                            lastString = lastString.Trim();
                        }
                        //lastString = lastString.Substring(1);

                        obj.data.Clear();
                        obj.handle.Set();
                    }
                }
                catch (Exception ex)
                {
                    obj.data.Clear();
                    Log(ErrorMsg(ex.Message));
                    obj.handle.Set();
                }
            }
            else
            {
                obj.client.Close();
                obj.handle.Set();
            }
        }

        /* Прослушивание авторизации. */
        private void ReadAuth(IAsyncResult result)
        {
            int bytes = 0;
            if (obj.client.Connected)
            {
                try
                {
                    bytes = obj.stream.EndRead(result);
                }
                catch (Exception ex)
                {
                    Log(ErrorMsg(ex.Message));
                }
            }
            if (bytes > 0)
            {
                obj.data.AppendFormat("{0}", Encoding.UTF8.GetString(obj.buffer, 0, bytes));
                try
                {
                    if (obj.stream.DataAvailable)
                    {
                        obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(ReadAuth), null);
                    }
                    else
                    {
                        JavaScriptSerializer json = new JavaScriptSerializer(); // feel free to use JSON serializer
                        Dictionary<string, string> data = json.Deserialize<Dictionary<string, string>>(obj.data.ToString());
                        if (data.ContainsKey("status") && data["status"].Equals("authorized"))
                        {
                            Connected(true);
                        }
                        obj.data.Clear();
                        obj.handle.Set();
                    }
                }
                catch (Exception ex)
                {
                    obj.data.Clear();
                    Log(ErrorMsg(ex.Message));
                    obj.handle.Set();
                }
            }
            else
            {
                obj.client.Close();
                obj.handle.Set();
            }
        }

        /* Авторизация пользователя. */
        private bool Authorize()
        {
            bool success = false;
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("username", obj.username);
            data.Add("key", obj.key);
            JavaScriptSerializer json = new JavaScriptSerializer(); // feel free to use JSON serializer
            Send(json.Serialize(data));
            while (obj.client.Connected)
            {
                try
                {
                    obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(ReadAuth), null);
                    obj.handle.WaitOne();
                    if (connected)
                    {
                        success = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log(ErrorMsg(ex.Message));
                }
            }
            if (!connected)
            {
                Log(SystemMsg("Unauthorized"));
            }
            return success;
        }

        /* Подключение пользователя. */
        private void Connection(IPAddress ip, int port, string username, string key)
        {
            try
            {
                obj = new MyClient();
                obj.username = username;
                obj.key = key;
                obj.client = new TcpClient();
                obj.client.Connect(ip, port);
                obj.stream = obj.client.GetStream();
                obj.buffer = new byte[obj.client.ReceiveBufferSize];
                obj.data = new StringBuilder();
                obj.handle = new EventWaitHandle(false, EventResetMode.AutoReset);
                if (Authorize())
                {
                    while (obj.client.Connected)
                    {
                        try
                        {
                            obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(Read), null);
                            obj.handle.WaitOne();
                        }
                        catch (Exception ex)
                        {
                            Log(ErrorMsg(ex.Message));
                        }
                    }
                    obj.client.Close();
                    Connected(false);
                }
            }
            catch (Exception ex)
            {
                Log(ErrorMsg(ex.Message));
            }
        }

        /* Подключение пользователя. */
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (connected)
            {
                obj.client.Close();
            }
            else if (client == null || !client.IsAlive)
            {
                string address = addrTextBox.Text.Trim();
                string number = portTextBox.Text.Trim();
                string username = usernameTextBox.Text.Trim();
                bool error = false;
                IPAddress ip = null;
                if (address.Length < 1)
                {
                    error = true;
                    Log(SystemMsg("Address is required"));
                }
                else
                {
                    try
                    {
                        ip = Dns.Resolve(address).AddressList[0];
                    }
                    catch
                    {
                        error = true;
                        Log(SystemMsg("Address is not valid"));
                    }
                }
                int port = -1;
                if (number.Length < 1)
                {
                    error = true;
                    Log(SystemMsg("Port number is required"));
                }
                else if (!int.TryParse(number, out port))
                {
                    error = true;
                    Log(SystemMsg("Port number is not valid"));
                }
                else if (port < 0 || port > 65535)
                {
                    error = true;
                    Log(SystemMsg("Port number is out of range"));
                }
                if (username.Length < 1)
                {
                    error = true;
                    Log(SystemMsg("Username is required"));
                }
                if (!error)
                {
                    // encryption key is optional
                    client = new Thread(() => Connection(ip, port, username, keyTextBox.Text))
                    {
                        IsBackground = true
                    };
                    client.Start();
                }
            }
        }

        /* Запись. */
        private void Write(IAsyncResult result)
        {
            if (obj.client.Connected)
            {
                try
                {
                    obj.stream.EndWrite(result);
                }
                catch (Exception ex)
                {
                    Log(ErrorMsg(ex.Message));
                }
            }
        }

        /* Отправить сообщение всем. */
        private void BeginWrite(string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            if (obj.client.Connected)
            {
                try
                {
                    obj.stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(Write), null);
                }
                catch (Exception ex)
                {
                    Log(ErrorMsg(ex.Message));
                }
            }
        }

        /* Отправление сообщений. */
        private void Send(string msg)
        {
            if (send == null || send.IsCompleted)
            {
                send = Task.Factory.StartNew(() => BeginWrite(msg));
            }
            else
            {
                send.ContinueWith(antecendent => BeginWrite(msg));
            }
        }

        /* Отправка сообщения по нажатию Enter */
        private void SendTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (sendTextBox.Text.Length > 0)
                {
                    string msg = sendTextBox.Text;
                    sendTextBox.Clear();
                    Log(string.Format("{0} (You): {1}", obj.username, msg));
                    if (connected)
                    {
                        Send(msg);
                    }
                }
            }
        }

        /* Событие закрытие формы. */
        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            exit = true;
            if (connected)
            {
                obj.client.Close();
            }
        }

        private void ClearButton_Click(object sender, EventArgs e) // очистка поля
        {
            Log();
        }

        /* Если чекбокс прожат, то ключ заменяются на звездочки. */
        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (keyTextBox.PasswordChar == '*')
            {
                keyTextBox.PasswordChar = '\0';
            }
            else
            {
                keyTextBox.PasswordChar = '*';
            }
        }

        /* Дешифрование. */
        private void decryptButton_Click(object sender, EventArgs e) // дешифрование
        {
            if (lastString.Length > 0 && encryptionKeyTextBox.Text.Length > 0)
            {
                _target = new PlayFairEng(encryptionKeyTextBox.Text);

                string actual = _target.Decrypt(lastString);

                Log(string.Format("Transcript of the message: {0}", actual));
            }
        }

        /* Шифрование. */
        private void encryptButton_Click(object sender, EventArgs e) // шифрование
        {
            _target = new PlayFairEng(encryptionKeyTextBox.Text);

            string actual = _target.Encrypt(sendTextBox.Text);
            sendTextBox.Text = actual;
        }

        /* Отправить ключ шифрования. */
        private void encryptionKeyTextBox_Click(object sender, EventArgs e)
        {
            if (encryptionKeyTextBox.Text.Length > 0)
            {
                string msg = encryptionKeyTextBox.Text;
                encryptionKeyTextBox.Clear();
                Send("0" + msg);
            }
        }

        /* Функция, проверяющая является ли строка ввода пустой. 
           Если да, то кнопка задизейблена. */
        private void sendTextBox_TextChanged(object sender, EventArgs e)
        {
            if (sendTextBox.Text.Length == 0)
            {
                encryptButton.Enabled = false;
            }
            else
            {
                encryptButton.Enabled = true;
            }
        }

        /* Нельзя вводить русские буквы. */
        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < 'А' || e.KeyChar > 'Я') && (e.KeyChar < 'а' || e.KeyChar > 'я'))
            {

            }
            else
            {
                e.Handled = true;
            }
        }
    }
}