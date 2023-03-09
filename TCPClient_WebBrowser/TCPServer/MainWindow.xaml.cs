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

namespace TCPClient
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        IPAddress adressIP;
        int port;
        string nazwa = "Serwer";
        private TcpClient client = null;
        static List<string> names = new List<string>();
        static List<Client> users = new List<Client>();
        BackgroundWorker bw0 = new BackgroundWorker();
        BackgroundWorker bw1 = new BackgroundWorker();
        public static MainWindow window0;
        private BinaryReader reading = null;
        private BinaryWriter writing = null;
        bool praca = false;
        private bool activeCall = false;
        private int licznik = 0;


        private string path = "index0.html";
        private string path1 = "";


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

            btnSend.IsEnabled = false;
        }

        public static void Delete(Client disconnectedClient)
        {
            names.Remove(disconnectedClient.nazwa);
            users.Remove(disconnectedClient);
            disconnectedClient.message.CancelAsync();
        }


        private void connect(object sender, DoWorkEventArgs e)
        {

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    btnStart.IsEnabled = false;
                    btnSend.IsEnabled = true;
                }));

            IPAddress adressIP = null;

            try
            {
                string nickHelp = "";
                string IPhelp = null;
                tbAddress.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => { IPhelp = tbAddress.Text; }));
                tbName.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => { nickHelp = tbName.Text; }));
                adressIP = IPAddress.Parse(IPhelp);
                char temp = char.Parse(nickHelp.Substring(0, 1));
                if (!(Char.IsLetter(temp)))
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() => {
                            tbName.Text = String.Empty;
                            btnStart.IsEnabled = false;
                        }));
                    MessageBox.Show("Nick musi zaczynać się od litery!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() => { btnStart.IsEnabled = true; }));
                    btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() => { btnSend.IsEnabled = false; }));
                    return;
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    tbAddress.Text = String.Empty;
                    btnStart.IsEnabled = false;
                }));
                MessageBox.Show("Błędny adres IP!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStart.IsEnabled = true; }));
                btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnSend.IsEnabled = false; }));
                return;
            }
            int port = 0;
            tbPort.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { port = Convert.ToInt16(tbPort.Text); }));

            try
            {
                btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnStart.IsEnabled = false; }));
                btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { btnSend.IsEnabled = true; }));
                client = new TcpClient(adressIP.ToString(), port);
                lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { lbMessages.Items.Add("Połączono..."); }));
                HtmlDocument html = new HtmlDocument();
                html.Load("index0.html");
                html.DocumentNode.SelectSingleNode("//body").InnerHtml = "<b>Połączono...</b><br>";
                html.Save("index0.html");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index0.html", dir));
                        wbChat.Navigate(sciezka);
                    }));
                NetworkStream ns = client.GetStream();
                reading = new BinaryReader(ns);
                writing = new BinaryWriter(ns);
                tbName.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { writing.Write(tbName.Text); }));
               
                activeCall = true;
                bw1 = new BackgroundWorker();
                praca = true;
                bw1.DoWork += (sender2, e2) =>
                {
                    string messageReceived;

                    try
                    {
                        messageReceived = reading.ReadString();
                        licznik = int.Parse(messageReceived);
                        try
                        {
                            path1 = "index"+licznik+".html";
                            File.Copy(path, path1);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Nie można utworzyć pliku!\n"+ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        while ((messageReceived = reading.ReadString()) != "END")
                        {
                            try
                            {
                                if(messageReceived == "<b>Serwer został zatrzymany...</b><br>")
                                {
                                    HtmlDocument html = new HtmlDocument();
                                    html.Load(path1);
                                    html.DocumentNode.SelectSingleNode("//body").InnerHtml = "<b>Serwer został zamknięty...</b><br>";
                                    html.Save(path1);
                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, 
                                        new Action(() => {
                                            string dir = Directory.GetCurrentDirectory();
                                            Uri sciezka = new Uri(String.Format("file:///{0}/" + path1, dir));
                                            wbChat.Navigate(sciezka);
                                        }));
                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        new Action(() => {

                                            lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                                                new Action(() => { lbMessages.Items.Add("Serwer został zamknięty..."); }));
                                            btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                                                new Action(() => { btnStart.IsEnabled = true; }));
                                            btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                                                new Action(() => { btnSend.IsEnabled = false; }));
                                        }));

                                    client.Close();
                                }
                                else
                                {
                                    foreach(char l in messageReceived)
                                    {
                                        if (Char.IsDigit(l))
                                        {
                                            messageReceived = messageReceived.Remove(0,1);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }


                                    lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        new Action(() => {
                                            lbMessages.Items.Add(messageReceived);
                                        }));

                                    HtmlDocument html = new HtmlDocument();
                                    html.Load(path1);
                                    html.DocumentNode.SelectSingleNode("//body").InnerHtml += $"{messageReceived}";
                                    html.Save(path1);

                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                                        new Action(() => {
                                            string dir = Directory.GetCurrentDirectory();
                                            Uri sciezka = new Uri(String.Format("file:///{0}/" + path1, dir));
                                            wbChat.Navigate(sciezka);
                                        }));
                                }
                            }
                            catch (Exception ex)
                            {
                                HtmlDocument html = new HtmlDocument();
                                html.Load(path1);
                                html.DocumentNode.SelectSingleNode("//body").InnerHtml = "<b>Serwer został zamknięty...</b><br>";
                                html.Save(path1);
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                                    new Action(() => {
                                        string dir = Directory.GetCurrentDirectory();
                                        Uri sciezka = new Uri(String.Format("file:///{0}/" + path1, dir));
                                        wbChat.Navigate(sciezka);
                                    }));
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                                    new Action(() => {

                                        lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                                            new Action(() => { lbMessages.Items.Add("Serwer został zamknięty..."); }));
                                        btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                                            new Action(() => { btnStart.IsEnabled = true; }));
                                        btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                                            new Action(() => { btnSend.IsEnabled = false; }));
                                    }));

                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        HtmlDocument html = new HtmlDocument();
                        html.Load(path1);
                        html.DocumentNode.SelectSingleNode("//body").InnerHtml = "<b>Serwer został zamknięty...</b><br>";
                        html.Save(path1);
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(() => {
                                string dir = Directory.GetCurrentDirectory();
                                Uri sciezka = new Uri(String.Format("file:///{0}/" + path1, dir));
                                wbChat.Navigate(sciezka);
                            }));
                        lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(() => { lbMessages.Items.Add("Serwer został zamknięty..."); }));
                        btnStart.Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(() => { btnStart.IsEnabled = true; }));
                        btnSend.Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(() => { btnSend.IsEnabled = false; }));
                    }

                    OnClosing(e);
                    client.Close();

                };
                bw1.WorkerSupportsCancellation = true;
                bw1.RunWorkerAsync();

            }
            catch (Exception ex)
            {
                HtmlDocument html = new HtmlDocument();
                html.Load("index0.html");
                html.DocumentNode.SelectSingleNode("//body").InnerHtml += "<b>Inicjacja serwera nie powiodła się!</b><br>";
                html.Save("index0.html");
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => {
                        string dir = Directory.GetCurrentDirectory();
                        Uri sciezka = new Uri(String.Format("file:///{0}/index0.html", dir));
                        wbChat.Navigate(sciezka);
                    }));
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => {
                    lbMessages.Items.Add("Inicjacja serwera nie powiodła się!");
                    btnStart.IsEnabled = false;
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

            

            if (System.IO.File.Exists(path1))
            {
                try
                {
                    System.IO.File.Delete(path1);
                }
                catch (System.IO.IOException ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }



        private void btnStart_Click_1(object sender, RoutedEventArgs e)
        {
            bw0.RunWorkerAsync();
        }


        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = tbMessages.Text;
            tbMessages.Clear();
            try
            {
                writing.Write(message);
            }
            catch
            {
                MessageBox.Show("Serwer jest wyłączony.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show("Przycisk czerwony (1): pisz na czerwono\nPrzycisk zielony (2): pisz na zielono\nPrzycisk niebieski (3): pisz na niebiesko\nPrzycisk \"i\" (4): tekst pochyły", "Pomoc", MessageBoxButton.OK, MessageBoxImage.Question);
        }

        private void tbAddress_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

