using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// HRD uses Kenwood command set
// Limite to freq,mode, and S-Meter, and a status command that returns freq, mode and rx/tx status
/*
 * Command Detail

 * FA Reads and sets the VFO A frequency in Hz
Set FA<11 digit frequency>;
Read FA;
Answer FA<11 digit frequency>;

 * FB Reads and sets the VFO B frequency in Hz
Set FB<11 digit frequency>;
Read FB;
Answer FB<11 digit frequency>;

 * IF Retrieves the transceiver status
Read IF;
Answer IFP1P2P3P4p5P6P7P8P9P10P11P12P13P14P15;
Where:
P1 = 11 digits, frequency in Hz
P2 = 5 digits, not used
P3 = 5 digits, not used
P4 = 1 digit, not used
P5 = 1 digit, not used
P6 = 1 digit, not used
P7 = 2 digits, not used
P8 = 1 digit, 0: RX, 1: TX
P9 = 1 digit, see MD above
P10 = 1 digit, not used
P11 = 1 digit, not used
P12 = 1 digit, not used
P13 = 1 digit, not used
P14 = 2 digits, not used
P15 = 1 digit, not used

 * MD Recalls or reads the operating mode status
Set MD<mode>;
Read MD;
Answer MD<mode>;
Where mode is one of:
0: None
1: LSB
2: USB
3: CW
4: FM
5: AM
6: FSK
7: CWR (CW Reverse)
8: Tune
9: FSR (FSK Reverse)

 * SM Retrieves the S-Meter value
Read SM;
Answer IF<4 digit value>;
Where the returned value range is from 0000 to 0030. Each unit is 1/2
an S-unit. S5 is 0010, S9 is 0018.
 */
namespace CWGen
{
    class Kenwood
    {
        SerialPort port;
        string errMsg = null;
        public Kenwood()
        {
            // do nothing
        }
        public bool Connect(string comport) { // returns true if Connect succeeds
            bool result = false;
            try
            {
                port = new SerialPort(comport);
                port.BaudRate = 9600;
                port.StopBits = System.IO.Ports.StopBits.Two;
                port.Parity = System.IO.Ports.Parity.None;
                port.Open();
                double f = Frequency();
                if (f > 0.0) // then we got a frequency result and we're working
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                errMsg = e.Message;
                return false;
            }
            return result;
        }
        public double Frequency() // returns MHz
        {
            double f = 0.0;
            port.Write("FA;\n");
            Thread.Sleep(200);
            if (port.BytesToRead > 0)
            {
                string s = port.ReadExisting();
                string[] tokens = s.Split(new [] {';','\r','\n'});
                string fa = tokens[0];
                if (fa.Substring(0, 2).Equals("FA"))
                {
                    f = Convert.ToDouble(fa.Substring(2));
                    f /= 1e6;
                }
            }
            return f;
        }
        public bool Frequency(double freq) // Input in MHz
        {
            bool result = false;
            int ifreq = (int)(freq * 1e6); // shoul round correcly
            string sf = ifreq.ToString("00000000000");
            string cmd = "FA" + sf + ";\n";
            port.Write(cmd);
            Thread.Sleep(100);
            string s = port.ReadExisting();
            if (s.Equals("?; "))
            {
                result = true;
            }
            return result;
        }
    }
}
