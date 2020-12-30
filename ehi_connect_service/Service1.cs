using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ehi_connect_service
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        TcpClient client;
        Int32 port = 5060;
        static String server = "192.168.66.50";
        static Socket clientSocket;
         ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _thread;

        protected override void OnStart(string[] args)
        {

            _thread = new Thread(authenticate);
            _thread.Name = "EHI Connect Service Thread";
            _thread.IsBackground = true;
            _thread.Start();
           // authenticate("192.168.66.50");
        }

        protected override void OnStop()
        {
            _shutdownEvent.Set();
            if (!_thread.Join(3000))
            { // give the thread 3 seconds to stop
                _thread.Abort();
            }
        }

        protected void authenticate()
        {
            while (!_shutdownEvent.WaitOne(0))
            {
                bool socketException = false;
            Connect:
                WriteToFile_C("Connecting to AMI session: 192.168.66.50 \n");

                // Connect to the asterisk server.
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint myEndPoint = new IPEndPoint(IPAddress.Any, 9900);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(server), 5038);
                utility _utility = new utility();
                try
                {

                    if (!socketException)
                    {
                        serverSocket.Bind(myEndPoint);
                        serverSocket.Listen(4);
                    }

                    clientSocket.Connect(serverEndPoint);

                    // Login to the server; manager.conf needs to be setup with matching credentials.
                    clientSocket.Send(Encoding.ASCII.GetBytes("Action:Login\r\nUsername: atladmin\r\nSecret: 7mmT@XAy\r\nActionID: 4\r\n\r\n"));


                    int bytesRead, bytes = 0;

                    do
                    {
                        byte[] buffer = new byte[10024];
                        byte[] buffer2 = new byte[10024];
                        bytesRead = clientSocket.Receive(buffer);

                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        if (!socketException)
                            serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), serverSocket);
                        //  bytes=socketAccept.Receive(buffer2);

                        string responseData = Encoding.ASCII.GetString(buffer2, 0, bytes);
                        //  WriteToFile(responseData);

                        String[] pars = response.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
                        if (response.IndexOf("\r\n\r\n") > -1)
                        {
                            WriteToFile(response);
                            Task task = new Task(() => _utility.consumeResponse(pars));
                            task.Start();
                        }
                        if (Regex.Match(response, "Message: Authentication accepted", RegexOptions.IgnoreCase).Success)
                        {
                            WriteToFile_C("Login Successfull");
                        }

                        //Let's get pretty parsing and checking events



                    } while (bytesRead != 0);

                    //  WriteToFile_C("Connection to server lost.");
                    //  _utility._sqlcon.Dispose();
                    // serverSocket.Shutdown(SocketShutdown.Both);
                    // serverSocket.Disconnect(true);
                    socketException = true;
                    //  goto Connect;
                    //Console.ReadLine();

                }
                catch (SocketException ex)
                {
                    // _utility._sqlcon.Dispose();
                    WriteToFile_C(ex.Message.ToString());
                    goto Connect;
                }
            }
        }


        public static void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }


        public static void WriteToFile_C(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ConnectivityLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            byte[] buffer = new byte[10024];
            byte[] buffer2 = new byte[10024];

            int bytesRead = handler.Receive(buffer);

            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            WriteToFile("SAID: " + response);
            clientSocket.Send(Encoding.ASCII.GetBytes(response));
        }


     
    }
}
