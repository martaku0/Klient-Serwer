using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TCPServer
{
    public class Client
    {
        public TcpClient client;
        public BackgroundWorker message;
        public String nazwa;
        public String connectionTime;
        public BinaryReader reading;
        public BinaryWriter writing;

        public Client(ref TcpClient newConnection, ref BinaryReader newReading, ref string newNick, string whenConnected)
        {
            client = newConnection;
            nazwa = newNick;
            connectionTime = whenConnected;
            reading = newReading;
            writing = new BinaryWriter(client.GetStream());

            message = new BackgroundWorker();
            message.DoWork += (sender, e) =>
            {
                while (true)
                {
                    try
                    {
                        TCPServer.MainWindow.Broadcast(reading.ReadString(), ref nazwa);
                    }
                    catch (Exception ex)
                    {
                        TCPServer.MainWindow.Delete(this);
                    }
                }
            };
            message.WorkerSupportsCancellation = true;
            message.RunWorkerAsync();

        }
    }
}
