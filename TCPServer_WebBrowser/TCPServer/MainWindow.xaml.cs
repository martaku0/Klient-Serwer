using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;
using HtmlAgilityPack;

namespace TCPServer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        IPAddress adressIP;
        int port;
        string nazwa = "Serwer";
        private TcpListener server = null;
        private TcpClient client = null;
        static List<String> names = new List<String>();
        static List<Client> users = new List<Client>();
        public static MainWindow window0;
        bool praca = false;
        BackgroundWorker bw0 = new BackgroundWorker();
        BackgroundWorker bw1 = new BackgroundWorker();
        private bool activeCall = false;
        private int licznik = 0;

        public MainWindow()
        {
            InitializeComponent();
            bw0.DoWork += new DoWorkEventHandler(connect);
            bw0.WorkerSupportsCancellation = true;
            bw1.WorkerSupportsCancellation = true;
            window0 = (MainWindow)Application.Current.MainWindow;
            
            MinHeight = 630;
            MaxHeight = 630;
            MinWidth = 536;
            MaxWidth = 536;

            btnStop.IsEnabled = false;
            btnSend.IsEnabled = false;
        }

        public static void Delete(Client disconnectedClient)
        {
            names.Remove(disconnectedClient.nazwa);
            users.Remove(disconnectedClient);
            disconnectedClient.message.CancelAsync();
        }

        public static void Broadcast(String message, ref String sender)
        {
            HtmlDocument html = new HtmlDocument();
            html.Load("index.html");
            string send = sender;
            foreach (Client client in users)
            {
                if (client.nazwa == sender)
                {
                    client.writing.Write($"<b>TY</b>: {message}<br>");
                    client.writing.Flush();
                }
                else
                {
                    client.writing.Write($"<b>{sender}</b>: {message}<br>");
                    client.writing.Flush();
                }
            }

            html.DocumentNode.SelectSingleNode("//body").InnerHtml += $"<b>{send}</b>: {message}<br>";

            html.Save("index.html");


            ((MainWindow)MainWindow.window0).wbChat.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string directory = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index.html", directory));
                        ((MainWindow)MainWindow.window0).wbChat.Navigate(sciezka); }));

            ((MainWindow)MainWindow.window0).lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { ((MainWindow)MainWindow.window0).lbMessages.Items.Add(send + ": " + message); }));

        }
        
        private void connect(object sender, DoWorkEventArgs e)
        {
            

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    btnStart.IsEnabled = false;
                    btnStop.IsEnabled = true;
                }));


            IPAddress adressIP = null;

            try
            {
                string IPhelp = null;
                tbAddress.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => { IPhelp = tbAddress.Text; }));
                adressIP = IPAddress.Parse(IPhelp);
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    tbAddress.Text = String.Empty;
                    btnStart.IsEnabled = false;
                    btnStop.IsEnabled = false;
                }));
                MessageBox.Show("Błędny adres IP!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStart.IsEnabled = true; }));
                return;
            }
            int port = 0;
            tbPort.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { port = Convert.ToInt16(tbPort.Text); }));

            try
            {
                server = new TcpListener(adressIP, port);
                server.Start();

                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStart.IsEnabled = false; }));
                btnStop.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStop.IsEnabled = true; }));
                btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnSend.IsEnabled = true; }));

                HtmlDocument html = new HtmlDocument();
                html.Load("index.html");

                lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { lbMessages.Items.Add("Serwer działa..."); }));

                html.DocumentNode.SelectSingleNode("//body").InnerHtml += "<b>Serwer działa...</b><br>";

                html.Save("index.html");

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index.html", dir));
                        wbChat.Navigate(sciezka);
                    }));
                
                bw1 = new BackgroundWorker();
                praca = true;
                bw1.WorkerSupportsCancellation = true;
                bw1.RunWorkerAsync();
                bw1.DoWork += (sender2, e2) =>
                {
                    while (true)
                    {

                        try
                        {
                            TcpClient tcpClient = server.AcceptTcpClient();
                            BinaryReader reading2 = new BinaryReader(tcpClient.GetStream());
                            String nazwa = reading2.ReadString();
                            while (names.Contains(nazwa))
                            {
                                nazwa += "-sob";
                            }
                            names.Add(nazwa);
                            users.Add(new Client(ref tcpClient, ref reading2, ref nazwa, DateTime.Now.ToString("h:mm:ss tt")));
                            string a = "";
                            licznik += 1;
                            foreach (Client client in users)
                            {
                                client.writing.Write($"{licznik}");
                            }
                            TCPServer.MainWindow.Broadcast($"<b>{nazwa} </b> dołączył(a)!", ref this.nazwa);

                        }
                        catch (Exception ex)
                        {
                            
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(() => {
                                btnStart.IsEnabled = false;
                                btnStop.IsEnabled = false;
                            }));
                                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                                    new Action(() => { btnStart.IsEnabled = true; }));
                                break;
                            
                        }

                    }

                };

            }
            catch (Exception ex)
            {
                HtmlDocument html = new HtmlDocument();
                html.Load("index.html");


                html.DocumentNode.SelectSingleNode("//body").InnerHtml += "<b>Inicjacja serwera nie powiodła się!</b><br>";

                html.Save("index.html");

                ((MainWindow)TCPServer.MainWindow.window0).wbChat.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index.html", dir));
                        ((MainWindow)TCPServer.MainWindow.window0).wbChat.Navigate(sciezka);
                    }));

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    lbMessages.Items.Add("Inicjacja serwera nie powiodła się!");
                    btnStart.IsEnabled = false;
                    btnStop.IsEnabled = false;
                }));
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStart.IsEnabled = true; }));
            }


        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (bw0.IsBusy)
            {
                MessageBoxResult exit = MessageBox.Show("Czy jesteś pewien?", "Wyjście", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (exit == MessageBoxResult.Yes)
                {
                    bw0.CancelAsync();
                    bw1.CancelAsync();
                    e.Cancel = false;
                }
            }
            else
            {
                e.Cancel = false;
            }
        }



        private void btnStart_Click_1(object sender, RoutedEventArgs e)
        {
            licznik = 0;
            bw0.RunWorkerAsync();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.Close();
            }

            if (praca)
            {
                server.Stop();
                bw0.CancelAsync();
                bw1.CancelAsync();

                HtmlDocument html = new HtmlDocument();
                html.Load("index.html");

                foreach (Client client in users)
                {
                    client.writing.Write("<b>Serwer został zatrzymany...</b><br>");
                }
                names.Clear();
                users.Clear();

                html.DocumentNode.SelectSingleNode("//body").InnerHtml += "<b>Serwer został zatrzymany...</b><br>";

                html.Save("index.html");

                lbMessages.Items.Add("Serwer został zatrzymany...");

                ((MainWindow)TCPServer.MainWindow.window0).wbChat.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index.html", dir));
                        ((MainWindow)TCPServer.MainWindow.window0).wbChat.Navigate(sciezka);
                    }));

                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
                btnSend.IsEnabled = false;


                praca = false;
            }


        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = tbMessages.Text;
            tbMessages.Clear();
            if (users.Count != 0)
            {
                TCPServer.MainWindow.Broadcast(message, ref nazwa);
            }
            else
            {
                lbMessages.Items.Add("Brak połączenia!");
                HtmlDocument html = new HtmlDocument();
                html.Load("index.html");


                html.DocumentNode.SelectSingleNode("//body").InnerHtml += "<b>Brak połączenia!</b><br>";

                html.Save("index.html");

                ((MainWindow)TCPServer.MainWindow.window0).wbChat.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index.html", dir));
                        ((MainWindow)TCPServer.MainWindow.window0).wbChat.Navigate(sciezka);
                    }));
            }
        }

        private void btRed_Click(object sender, RoutedEventArgs e)
        {
            tbMessages.Text += "<span style=\"color:red\"></span>";
        }

        private void btGreen_Click(object sender, RoutedEventArgs e)
        {
            tbMessages.Text += "<span style=\"color:green\"></span>";
        }
        private void btBlue_Click(object sender, RoutedEventArgs e)
        {
            tbMessages.Text += "<span style=\"color:blue\"></span>";
        }

        private void btItalic_Click(object sender, RoutedEventArgs e)
        {
            tbMessages.Text += "<i></i>";
        }

        private void btHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Przycisk czerwony (1): pisz na czerwono\nPrzycisk zielony (2): pisz na zielono\nPrzycisk niebieski (3): pisz na niebiesko\nPrzycisk \"i\" (4): tekst pochyły\nPrzycisk \"b\" (5): tekst pogrubiony", "Pomoc", MessageBoxButton.OK, MessageBoxImage.Question);
        }

        private void btBold_Click_1(object sender, RoutedEventArgs e)
        {
            tbMessages.Text += "<b></b>";
        }
    }
}

