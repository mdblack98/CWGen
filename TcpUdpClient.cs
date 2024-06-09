using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CWGen
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    public class TcpUdpClient
    {
        public enum clientType { TCP, UDP }; //Type of connection the client is making.
        private const int ANYPORT = 0;
        private const int SAMPLETCPPORT = 4567;
        private const int SAMPLEUDPPORT = 4568;
        public clientType cliType;
        private bool DONE = false;

        public TcpUdpClient(clientType CliType)
        {
            this.cliType = CliType;
        }
        public void TcpClient(String serverName, String whatEver)
        {
            try
            {
                //Create an instance of TcpClient.
                TcpClient tcpClient = new TcpClient(serverName, SAMPLETCPPORT);
                //Create a NetworkStream for this tcpClient instance.
                //This is only required for TCP stream.
                NetworkStream tcpStream = tcpClient.GetStream();
                if (tcpStream.CanWrite)
                {
                    Byte[] inputToBeSent = System.Text.Encoding.ASCII.GetBytes(whatEver.ToCharArray());
                    tcpStream.Write(inputToBeSent, 0, inputToBeSent.Length);
                    tcpStream.Flush();
                }
                while (tcpStream.CanRead && !DONE)
                {
                    //We need the DONE condition here because there is possibility that
                    //the stream is ready to be read while there is nothing to be read.
                    if (tcpStream.DataAvailable)
                    {
                        Byte[] received = new Byte[512];
                        int nBytesReceived = tcpStream.Read(received, 0, received.Length);
                        String dataReceived = System.Text.Encoding.ASCII.GetString(received);
                        Console.WriteLine(dataReceived);
                        DONE = true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception has occurred.");
                Console.WriteLine(e.ToString());
            }
        }
        public void UdpClient(String serverName, String whatEver)
        {
            try
            {
                //Create an instance of UdpClient.
                UdpClient udpClient = new UdpClient(serverName, SAMPLEUDPPORT);
                Byte[] inputToBeSent = new Byte[256];
                inputToBeSent = System.Text.Encoding.ASCII.GetBytes(whatEver.ToCharArray());
                IPHostEntry remoteHostEntry = Dns.GetHostEntry(serverName);
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteHostEntry.AddressList[0], SAMPLEUDPPORT);
                int nBytesSent = udpClient.Send(inputToBeSent, inputToBeSent.Length);
                Byte[] received = new Byte[512];
                received = udpClient.Receive(ref remoteIpEndPoint);
                String dataReceived = System.Text.Encoding.ASCII.GetString(received);
                Console.WriteLine(dataReceived);
                udpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred!");
                Console.WriteLine(e.ToString());
            }
        }
        /*
        public static void Main(String[] argv)
        {
            if (argv.Length < 3)
            {
                Console.WriteLine("Usage: TcpUdpClient <TCP or UDP> <Server Name or IP Address> Message");
                Console.WriteLine("Example: TcpUdpClient TCP localhost ''hello. how are you?''");
            }
            else if ((argv[0] == "TCP") || (argv[0] == "tcp"))
            {
                TcpUdpClient stc = new TcpUdpClient(clientType.TCP);
                stc.sampleTcpClient2(argv[1], argv[2]);
                Console.WriteLine("The TCP server is disconnected.");
            }
            else if ((argv[0] == "UDP") || (argv[0] == "udp"))
            {
                TcpUdpClient suc = new TcpUdpClient(clientType.UDP);
                suc.sampleUdpClient2(argv[1], argv[2]);
                Console.WriteLine("The UDP server is disconnected.");
            }
        }
        */
    }
} 
