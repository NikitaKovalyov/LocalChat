using playfairСipher;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public class Server
    {
        private bool active = false;
        private Thread listener = null;
        private long id = 0;
        private struct MyClient
        {
            public long id;
            public StringBuilder username;
            public TcpClient client;
            public NetworkStream stream;
            public byte[] buffer;
            public StringBuilder data;
            public EventWaitHandle handle;
        };
        private ConcurrentDictionary<long, MyClient> clients = new ConcurrentDictionary<long, MyClient>();
        private Task send = null;
        private Thread disconnect = null;
        private bool exit = false;
        public SecurityAlgorithm _target;

        /* Логирование, если параметр не передается, то лог очищается. */
        public static void Log(TextBox logTextBox, TextBox encryptionKeyTextBox, string msg = "")
        {
            string[] tmp = msg.Split(':');

            logTextBox.Invoke((MethodInvoker)delegate
            {
                if (msg.Length > 0)
                {
                    string key = tmp[1].Trim();
                    if ((key[0] == '0'))
                    {
                        encryptionKeyTextBox.Text = key.Substring(1);
                    }
                    else
                    {
                        logTextBox.AppendText(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"),
                            msg, Environment.NewLine));
                    }
                }
                else
                {
                    logTextBox.Clear();
                }
            });
        }
    }
}
