using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CWGen
{
    class OmniVII
    {
        SerialPort radio = null;
        UdpClient iradio = null;
        public int freqa,freqb;
        Queue<byte> reply;
        // We have to pad cmds via the network interface
        string zeros = "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
        public OmniVII()
        {
        }
        public OmniVII(string comport)
        {
            reply = new Queue<byte>();
            freqa = freqb = 0;
            if (radio == null)
            {
                radio = new SerialPort();
                radio.PortName = comport;
                radio.BaudRate = 57600;
                radio.Handshake = (Handshake)Enum.Parse(typeof(Handshake), "RequestToSend", true);
                radio.ReadTimeout = 50;
                radio.Open();

                //foreach (string s in Enum.GetNames(typeof(Handshake))){
                //    handshake = (Handshake)Enum.Parse(typeof(Handshake),s,true);
                //}
                //radio.Handshake = handshake;
            }

        }
        public OmniVII(string ipaddr, int port)
        {
            reply = new Queue<byte>();
            freqa = freqb = 0;
            if (iradio == null)
            {
                iradio = new UdpClient();
                iradio.Connect("192.168.1.147", 49152);
            }
        }
        Queue<byte> cmd(string rcmd)
        {
            reply.Clear();
            if (radio != null) // Then we're on a COM port
            {
                radio.Write(rcmd + "\r");
                Thread.Sleep(100);
                while (radio.BytesToRead > 0)
                {
                    try
                    {
                        reply.Enqueue(Convert.ToByte(radio.ReadChar()));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else // We are network connected
            {
                Byte[] cmd = Encoding.ASCII.GetBytes("\0\0" + rcmd + "\r" + zeros.Substring(rcmd.Length));
                iradio.Send(cmd, cmd.Length);
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                Byte[] response = iradio.Receive(ref ep);
                foreach (Byte b in response)
                {
                    reply.Enqueue(b);
                }
            }
            return reply;
        }
        private Int32 radioFrequency()
        {
            int b3 = reply.Dequeue();
            int b2 = reply.Dequeue();
            int b1 = reply.Dequeue();
            int b0 = reply.Dequeue();
            //reply.Dequeue(); // Toss out <CR>
            int ifreq = (b3 << 24) | (b2 << 16) | (b1 << 8) | b0;
            //ifreq += freqa;
            return ifreq;
        }
        private Int32 power()
        {
            Queue<byte> reply = cmd("?");
            char c = (char)reply.Dequeue();
            c = (char)reply.Dequeue();
            return 0;
        }
        private string settings()
        {
            string mysettings = "";
            Queue<byte> reply = cmd("?*");
            while (reply.Count > 0)
            {
                char first = (char)reply.Dequeue();
                switch (first)
                {
                    case 'A':
                        freqa = radioFrequency();
                        reply.Dequeue();
                        mysettings = "FREQA=" + freqa/1000.0 + "\n";
                        break;
                    case 'B':
                        freqb = radioFrequency();
                        reply.Dequeue();
                        mysettings += "FREQB=" + freqb / 1000.0 + "\n";
                        break;
                    case 'C':
                        char c2 = (char)reply.Dequeue();
                        char c3 = (char)reply.Dequeue();
                        string s = ""+c2+c3;
                        if (s.Equals("1A")) {
                            mysettings += "AudioSrc=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1B"))
                        {
                            mysettings += "KeyLoop=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1C"))
                        {
                            mysettings += "CW Rise/Fall=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1D"))
                        {
                            mysettings += "MicGain=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1E"))
                        {
                            mysettings += "LineGain=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1F"))
                        {
                            mysettings += "SpchProc=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1G"))
                        {
                            mysettings += "FMCTCSSTone=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1H"))
                        {
                            mysettings += "RXEqualizer=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1I"))
                        {
                            mysettings += "TXEqualizer=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1J"))
                        {
                            mysettings += "TXRollOff=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1K"))
                        {
                            mysettings += "T/R Delay=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1L"))
                        {
                            mysettings += "SidetoneFreq=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1M"))
                        {
                            mysettings += "CWQSKDelay=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1N"))
                        {
                            mysettings += "TXEnable=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1O"))
                        {
                            mysettings += "SidebandTXBW=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1P"))
                        {
                            mysettings += "AutoTuner=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1Q"))
                        {
                            mysettings += "SidetoneVol=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1R"))
                        {
                            mysettings += "SpotVol=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1S"))
                        {
                            mysettings += "FSKMarkHi/LO=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1T"))
                        {
                            mysettings += "I-FFilterSel=" + reply.Dequeue()+"/"+reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1U"))
                        {
                            mysettings += "I-FFilterEnable=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1V"))
                        {
                            mysettings += "Antenna=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1W"))
                        {
                            mysettings += "Monitor=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1X"))
                        {
                            mysettings += "Power/Fwd/Ref=" + reply.Dequeue() + "/" + reply.Dequeue() + "/" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1Y"))
                        {
                            mysettings += "SPOT=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("1Z"))
                        {
                            mysettings += "PreAmp=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2A"))
                        {
                            mysettings += "Tuner=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2B"))
                        {
                            mysettings += "Split=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2C"))
                        {
                            mysettings += "VOXTRIP=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2D"))
                        {
                            mysettings += "ANTIVOX=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2E"))
                        {
                            mysettings += "VOXHang=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2F"))
                        {
                            mysettings += "CWKeyerMode=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2G"))
                        {
                            mysettings += "CWWeighting=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2H"))
                        {
                            mysettings += "Notch=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2I"))
                        {
                            mysettings += "NotchCenterFreq=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2J"))
                        {
                            mysettings += "NotchWidth=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2K"))
                        {
                            mysettings += "CWCharTx=" + (char)reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2L"))
                        {
                            mysettings += "KeyerSpeed=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2M"))
                        {
                            mysettings += "VOX=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2N"))
                        {
                            mysettings += "Display=" + (char)reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2O"))
                        {
                            mysettings += "SpkrMute=" + (char)reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else if (s.Equals("2P"))
                        {
                            mysettings += "TRIPGain=" + reply.Dequeue() + "\n";
                            reply.Dequeue(); // toss <CR>
                        }
                        else
                        {
                            MessageBox.Show("Oops? " + s);
                        }
                        break;
                    case 'F': 
                        reply.Dequeue(); // what is this thing? 0500 in ASCII
                        reply.Dequeue();
                        reply.Dequeue();
                        reply.Dequeue();
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'G':
                        mysettings += "AGCMODE=" + (char)reply.Dequeue() + "\n";
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'H':
                        mysettings += "SQUELCH=" + reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'I':
                        mysettings += "RFGAIN=" + (char)reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'J':
                        mysettings += "ATTENUATOR=" + (char)reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'K':
                        mysettings += "NB=" + reply.Dequeue() + "\n";
                        mysettings += "NR=" + reply.Dequeue() + "\n";
                        mysettings += "AN=" + reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'L':
                        int rit = reply.Dequeue();
                        int xit1 = reply.Dequeue();
                        int xit0 = reply.Dequeue();
                        int xit = (xit1 << 8) | xit0;
                        reply.Dequeue(); // toss <CR>
                        mysettings += "RIT/XIT=" + rit + "/" + xit + "\n";
                        break;
                    case 'M':
                        mysettings += "VFOMODEA=" + (char)reply.Dequeue() + "\n";
                        mysettings += "VFOMODEB=" + (char)reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'N':
                        mysettings += "SPLIT=" + reply.Dequeue() + "\n";
                        reply.Dequeue();
                        break;
                    case 'P':
                        int b1 = reply.Dequeue();
                        int b0 = reply.Dequeue();
                        reply.Dequeue(); // toss <CR>
                        int passband = (b1<<8)|b0;
                        mysettings += "PASSBAND=" + passband + "\n";
                        break;
                    case 'R':
                        mysettings += "ORIONMODE?=" + (char)reply.Dequeue()+(char)reply.Dequeue()+"\n";
                        reply.Dequeue(); // toss <CR>
                        break;
                    case '@':
                        char second = (char)reply.Dequeue();
                        char m = (char)reply.Dequeue();
                        m = (char)reply.Dequeue();
                        mysettings += "ORIONMODE2=" + (char)reply.Dequeue()+"\n";
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'S':
                        int d1 = reply.Dequeue();
                        if((d1 & 0x80)!=0) {
                            // Forward/Reflected Power
                            mysettings += "FWD/REF="+ reply.Dequeue()+"/"+reply.Dequeue()+"\n";
                        }
                        else {
                            mysettings += "SUNITs/DB="+(char)d1+(char)reply.Dequeue()+"/"+(char)reply.Dequeue()+(char)reply.Dequeue()+"\n";
                        }
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'T':
                        mysettings += "Transmit=" + (char)reply.Dequeue();
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'U':
                        mysettings += "AF=" + reply.Dequeue()+"\n";
                        reply.Dequeue(); // toss <CR>
                        break;
                    case 'V':
                        string version = "V";
                        char c;
                        while((c = (char)reply.Dequeue())!=0) {
                            version += c;
                        }
                        mysettings += version + "\n";
                        reply.Dequeue(); // toss <CR>
                        reply.Dequeue(); // toss 2nd <CR>
                        break;
                    case 'W':
                        mysettings += "DSPFILTER=" + reply.Dequeue()+"\n";
                        reply.Dequeue();
                        break;
                    default:
                        string q = "";
                        foreach (byte b in reply)
                        {
                            q += (char)b;
                        }
                        //reply.Dequeue(); // toss <CR>
                        break;
                }
            }
            return mysettings;
        }
        public string test()
        {
            string mysettings = settings();
            //Int32 freq = radioFrequency();
            //Int32 p = power();
            //return Convert.ToString(freq);
            return mysettings;
            /*
            string zeros = "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
            string result = "Testing";
            UdpClient client = new UdpClient();
            client.Connect("192.168.1.147", 49152);
            string msg = "?*\r";
            Byte[] cmd = Encoding.ASCII.GetBytes("\0\0"+msg + zeros.Substring(msg.Length));
            client.Send(cmd, cmd.Length);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any,0);
            Byte[] response = client.Receive(ref ep);
            client.Close();
            return result;
            */
        }
    }
}
