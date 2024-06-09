using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace CWGen
{
    public class TcpUdpServer
    {
        private const int tcpPort = 5001;
        private const int udpPort = 5002;
        public Thread tcpThread, udpThread;
        Boolean stopThread = false;
        TcpListener tcpListener;

        public void TcpServer()
        {
            try
            {
                //Starting the TCP Listener thread.
                tcpThread = new Thread(new ThreadStart(StartListen2));
                tcpThread.Start();
                Console.WriteLine("Started TcpUdpServer's TCP Listener Thread!\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("An TCP Exception has occurred!" + e.ToString());
                tcpThread.Abort();
            }
        }

        public void close() 
        {
            stopThread = true;                      
            if (tcpThread != null)
            {
                tcpListener.Stop();               
                //tcpThread.Abort();
            }
            if (udpThread != null)
            {
                udpThread.Abort();
            }
        }

        public void UdpServer()
        {
            try
            {
                //Starting the UDP Server thread.
                udpThread = new Thread(new ThreadStart(StartReceiveFrom2));
                udpThread.Start();
                Console.WriteLine("Started TcpUdpServer's UDP Receiver Thread!\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("An UDP Exception has occurred!" + e.ToString());
                udpThread.Abort();
            }
        }
        /*
        public static void Main(String[] argv)
        {
            SampleTcpUdpServer2 sts = new SampleTcpUdpServer2();
        }
         * */
        public void StartListen2()
        {
            //Create an instance of TcpListener to listen for TCP connection.
            IPAddress addr = Dns.GetHostEntry("localhost").AddressList[0];
            //TcpListener tcpListener = new TcpListener(addr,tcpPort);
            tcpListener = new TcpListener(addr,tcpPort);
            while (true)
            {
                try
                {
                    tcpListener.Start();
                    //Program blocks on Accept() until a client connects.
                    Socket soTcp = tcpListener.AcceptSocket();
                    Byte[] received = new Byte[512];
                    String dataReceived = String.Empty;
                    int bytesReceived = 0;
                    do {
                        bytesReceived = soTcp.Receive(received, received.Length, 0);
                        dataReceived = System.Text.Encoding.ASCII.GetString(received,0,bytesReceived);
                        String returningString = dataReceived;
                        if (returningString.Length == 0) continue;
                        Byte[] returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                        String cmd = returningString.Substring(0, 1);
                        if (cmd.Equals("t"))
                        {
                            Form1.remoteCommand = 't';
                            returningString = "\r\nTransmitting\r\n";
                            while (Form1.remoteCommand == 't') Thread.Sleep(50);
                        }
                        else if (cmd.Equals("s"))
                        {
                            returningString = "\r\nStopping\r\n";
                            Form1.remoteCommand = 's';
                        }
                        returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                        //Returning a confirmation string back to the client.
                        //soTcp.Send(returningByte, returningByte.Length, 0);

                    } while(!dataReceived.Contains("q"));
                    soTcp.Disconnect(false);
                    tcpListener.Stop();
                }
                catch (SocketException se)
                {
                    tcpListener.Stop();
                    if (stopThread) return;

                    MessageBox.Show("A Socket Exception has occurred!" + se.ToString());
                }
            }
        }
        public void StartReceiveFrom2()
        {
            IPHostEntry localHostEntry;
            try
            {
                //Create a UDP socket.
                Socket soUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                }
                catch (Exception)
                {
                    Console.WriteLine("Local Host not found"); // fail
                    return;
                }
                IPEndPoint localIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], udpPort);
                soUdp.Bind(localIpEndPoint);
                while (true)
                {
                    Byte[] received = new Byte[256];
                    IPEndPoint tmpIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], udpPort);
                    EndPoint remoteEP = (tmpIpEndPoint);
                    int bytesReceived = soUdp.ReceiveFrom(received, ref remoteEP);
                    String dataReceived = System.Text.Encoding.ASCII.GetString(received);
                    Console.WriteLine("SampleClient is connected through UDP.");
                    Console.WriteLine(dataReceived);
                    String returningString = "The Server got your message through UDP:" + dataReceived;
                    Byte[] returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                    String cmd = returningString.Substring(0, 1);
                    if (cmd.Equals("t"))
                    {
                        Form1.remoteCommand = 't';
                        returningString = "\r\nTransmitting\r\n";
                    }
                    else if (cmd.Equals("s"))
                    {
                        returningString = "\r\nStopping\r\n";
                        Form1.remoteCommand = 's';
                    }
                    returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                    soUdp.SendTo(returningByte, remoteEP);
                }
            }
            catch (SocketException se)
            {
                if (stopThread) return;
                Console.WriteLine("A Socket Exception has occurred!" + se.ToString());
            }
        }
    }
}
