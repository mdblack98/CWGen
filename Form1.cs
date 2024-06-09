using AudioInterface;
using AxWMPLib;
using NAudio;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace CWGen
{
    // hamlib
    public partial class Form1 : Form
    {
        private ushort volume = 50;
        public string ipaddr = "192.168.1.147";
        private int soundDevice = 0;
        private List<string> soundCards = new List<string>();
        private string wavPath = null;
        private WaveOut waveOut2 = null;
        private Config config = new Config();
        private int volumePct = 100;
        private bool wavInit = false;
        private WaveMixerStream32 mixer = new WaveMixerStream32();
        private AudioSample audioSample;
        private double hz;
        private TcpUdpServer tcpserver;
        static public char remoteCommand = '?';
        private AxWindowsMediaPlayer axWindowsMediaPlayer2;

        public Form1()
        {
            InitializeComponent();
            mixer.AutoStop = false;
            tcpserver = new TcpUdpServer();
            tcpserver.TcpServer();
            tcpserver.UdpServer();
            axWindowsMediaPlayer2 = new AxWMPLib.AxWindowsMediaPlayer();
            axWindowsMediaPlayer2.BeginInit();
            axWindowsMediaPlayer2.EndInit();
            //axWindowsMediaPlayer2.Ctlenabled = true;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(this.buttonConfig, "Configuration dialog");
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            //this.Width = Properties.Settings.Default.Width;
            //this.Height = Properties.Settings.Default.Height;
            if (this.Location.X < 0 || this.Location.Y < 0)
            {
                this.Top = 0;
                this.Left = 0;
                //this.Width = 300;
                //this.Height = 400;
            }
            Rectangle rect = SystemInformation.VirtualScreen;
            if (this.Location.X > rect.Width || this.Location.Y > rect.Bottom)
            {
                this.Top = 0;
                this.Left = 0;
            }
            soundDevice = config.soundDevice();
            if (soundDevice < 0)
            {
                config.ShowDialog();
            }
            volumePct = config.VolumePct();
            hz = config.Hz();
            wavPath = System.Environment.GetEnvironmentVariable("TEMP")+"\\sine.wav";
            makeWave();
            waveOut2 = new WaveOut();
            waveOut2.DeviceNumber = soundDevice;
            waveOut2.Init(mixer);
            //audioSample = new AudioSample(wavPath);
            //audioSample.SetLoop(true);
            //mixer.AddInputStream(audioSample);
            int rate = mixer.WaveFormat.SampleRate;

            volume = Properties.Settings.Default.Volume;

            //axWindowsMediaPlayer2.settings.volume = volume;

            trackBarVolume.Value = volume;
            trackBarVolume.Refresh();
            waveOut2.Volume = trackBarVolume.Value / 100.0f;
            label2.Text = waveOut2.Volume * 100 + "%";
        }

        private void makeWave()
        {
            if (wavInit)
            {
                mixer.RemoveInputStream(audioSample);
                audioSample.Dispose();
                audioSample.Close();
            }
            wavInit = true;
            makeSineWave(config.Hz(), config.Seconds()*1000);
            audioSample = new AudioSample(wavPath);
            audioSample.SetLoop(true);
            mixer.AddInputStream(audioSample);
        }
        private void makeSineWave2(double frequency, int msDuration)
        {
            const double TAU = 2 * Math.PI;
            int samplesPerSecond = 48000;
            int samples = (int)((decimal)samplesPerSecond * msDuration / 1000);
            var ms = new MemoryStream();
            var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(48000, 16, 2));
            {
                {
                    int min = 100000;
                    int max = -100000;
                    double theta = frequency * TAU / (double)samplesPerSecond;
                    // 'volume' is UInt16 with range 0 thru Uint16.MaxValue ( = 65 535)
                    // we need 'amp' to have the range of +/-0 thru Int16.MaxValue ( = 32 767)
                    double volumePct = config.VolumePct();
                    //volumePct = 50;
                    double amp = (Int16.MaxValue) * (volumePct / 100.0);
                    //double amp = 40000 >> 2; // so we simply set amp = volume / 2
                    Random myRand = new Random();
                    for (int step = 0; step < samples; step++)
                    {
                        //short s =  (short)(((amp * Math.Sin(theta * step)+1)*65535/2)-32768);
                        double rand = myRand.NextDouble();
                        //short s =  (short)(Math.Sin(theta * step)*(amp/2)+((rand-0.5)*2)*(amp/2));
                        short s = (short)(Math.Sin(theta * step) * (amp / 2));
                        //if (s > 32700) s = 32700;
                        //if (s < -32700) s = -32700;
                        if (s < min) min = s;
                        if (s > max) max = s;
                        writer.Write(BitConverter.GetBytes(s),0,2);
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    FileStream file = new FileStream(wavPath, FileMode.Create, FileAccess.Write);
                    ms.WriteTo(file);
                    file.Close();
                    //MessageBox.Show("Min=" + min + "\nMax=" + max);
                    label1.Text = "Min = " + min + "\nMax = " + max;
                }
                writer.Close();
                writer.Dispose();
            }
        }
        private void makeSineWave(double frequency, int msDuration)
        {
            var mStrm = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(mStrm);
            const double TAU = 2 * Math.PI;
            int formatChunkSize = 16;
            int headerSize = 8;
            ushort formatType = 0x01;
            short tracks = 2;
            int samplesPerSecond = 48000;
            short bitsPerSample = 16;
            //short frameSize = (short)(tracks * ((bitsPerSample + 7) / 8));
            short frameSize = (short)((bitsPerSample * tracks) / 8);
            int bytesPerSecond = (bitsPerSample * tracks)/8;
            int waveSize = 2;
            int samples = (int)((decimal)samplesPerSecond * msDuration / 1000);
            int dataChunkSize = samples * tracks;
            int fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize + 2;
            // var encoding = new System.Text.UTF8Encoding();
            
            // 1-4
            writer.Write(0x46464952); // = encoding.GetBytes("RIFF")
            // 5-8
            writer.Write(fileSize);
            // 9-12
            writer.Write(0x45564157); // = encoding.GetBytes("WAVE")
            // 13-16
            writer.Write(0x20746D66); // = encoding.GetBytes("fmt ")
            // 17-20 = 16
            writer.Write(formatChunkSize);
            // 21-22 = 1 = PCM
            writer.Write(formatType);
            // 23-24 = 2 = # of channels
            writer.Write(tracks);
            // 25-28 = 48000
            writer.Write(samplesPerSecond);
            // 29-32
            writer.Write(bytesPerSecond);
            // 33-34
            writer.Write(frameSize);
            // 35-36
            writer.Write(bitsPerSample);
            //writer.Write(0x5453494c); // "LIST" 
            writer.Write(0x61746164); // = encoding.GetBytes("data")
            writer.Write(dataChunkSize);
            /*
            // from mmreg.h
            writer.Write(formatChunkSize);
            ushort uformatTag = 0xfffe;
            ushort nChannels = 1;
            ulong nSamplesPerSec = 48000;
            ulong nAvgBytesPerSec = 48000 * 2;
            ushort nBlockAlign = 2;
            ushort wBitsPerSample = 16;
            ushort cbSize = 0;
            writer.Write(0x46464952); // = encoding.GetBytes("RIFF")
            writer.Write(fileSize);
            writer.Write(0x45564157); // = encoding.GetBytes("WAVE")
            writer.Write(0x20746D66); // = encoding.GetBytes("fmt ")
            writer.Write(uformatTag);
            writer.Write(nChannels);
            writer.Write(nSamplesPerSec);
            writer.Write(nAvgBytesPerSec);
            writer.Write(nBlockAlign);
            writer.Write(wBitsPerSample);

            writer.Write(cbSize
);
            */
            {
                int min = 100000;
                int max = -100000;
                double theta = frequency * TAU / (double)samplesPerSecond;
                // 'volume' is UInt16 with range 0 thru Uint16.MaxValue ( = 65 535)
                // we need 'amp' to have the range of +/-0 thru Int16.MaxValue ( = 32 767)
                double volumePct = config.VolumePct();
                //volumePct = 50;
                double amp = (Int16.MaxValue) * (volumePct / 100.0);
                //double amp = 40000 >> 2; // so we simply set amp = volume / 2
                Random myRand = new Random();
                bool toggle = false;
                for (int step = 0; step < samples; step++) 
                {
                    int mymod = step % 100000;
                    if (mymod == 0)
                        if (toggle)
                        {
                            toggle = false;
                            amp *= 1;
                        }
                        else
                        {
                            toggle = true;
                            amp /= 1;
                        }
                    //short s =  (short)(((amp * Math.Sin(theta * step)+1)*65535/2)-32768);
                    double rand = (myRand.NextDouble()-0.5)*1.0;
                    //short s = (short)(Math.Sin(theta * step / 2) * (amp / 2) + rand * amp / 2);
                    short s = (short)(Math.Sin(theta * step / 2) * amp);
                    double amp2 = 0.25+Math.Abs(Math.Sin(step / 48000 * .03));
                    //s = (short)(s * amp2);  // varying fading
                    //if (s > 32700) s = 32700;
                    //if (s < -32700) s = -32700;
                    //if (s < 0) s /= 2;
                    if (s < min) min = s;
                    if (s > max) max = s;
                    writer.Write(s);
                }
                //MessageBox.Show("Min=" + min + "\nMax=" + max);
                label1.Text = "Min = " + min + "\nMax = " + max;
            }

            mStrm.Seek(0, SeekOrigin.Begin);
            FileStream file = new FileStream(wavPath, FileMode.Create, FileAccess.Write);
            mStrm.WriteTo(file);
            file.Close();
            return;
            /*
            WaveOut player = new WaveOut();
            //player.DeviceNumber = 0;

            player.Play();
            new System.Media.SoundPlayer(mStrm).Play();

            writer.Close();
            mStrm.Close();
             * */
        }

        private void playBackStopped(object sender, EventArgs e)
        {
            Transmit.Text = "Transmit";
            //waveOut2.PlaybackStopped -= new EventHandler<NAudio.Wave.StoppedEventArgs>(playBackStopped);
            //waveOut2.Dispose();
            //waveOut2 = null;
        }
        public void PlayBeep(double frequency, int msDuration, UInt16 volume = 16383)
        {
            int method = 1;
            if (method == 0)
            {
                //sineWave(1000);
                Thread.Sleep(msDuration);
                //sineWave();
            }
            else if (method == 1)
            {
                if (Transmit.Text.Equals("Transmit"))
                {
                    Transmit.Text = "Stop";
                    //waveOut2 = new WaveOut();
                    waveOut2.Play();
                    if (config.Seconds() > 0)
                    {
                        timer1.Interval = config.Seconds() * 1000;
                        timer1.Enabled = true;
                        timer1.Start();
                    }
                }
                else
                {
                    Transmit.Text = "Transmit";
                    waveOut2.Pause();
                    //waveOut2.Dispose();
                    //waveOut2 = null;
                }
            }
            else // Play WAV file
            {
                if (Transmit.Text.Equals("Transmit")) 
                {
                    string wavFilePath = @"C:\Temp\sine.wav";
                    /*
                    SoundPlayer player = new SoundPlayer(wavFilePath);
                    player.Play();
                    Thread.Sleep(3000);
                    player.Stop();
                    player.Dispose();
                     * */
                    //axWindowsMediaPlayer1.Visible = false;
                    axWindowsMediaPlayer2.URL = wavFilePath;
                    axWindowsMediaPlayer2.Ctlcontrols.play();
                }
                else
                {
                    axWindowsMediaPlayer2.Ctlcontrols.stop();
                }
            }
           
        }

        private void axWindowsMediaPlayer1_StatusChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == 3)
            {
                Transmit.Text = "Stop";
            }
            else
            {
                Transmit.Text = "Transmit";
            }
        }

        public void transmit()
        {
            if (soundDevice != config.soundDevice())
            {
                waveOut2.PlaybackStopped -= new EventHandler<NAudio.Wave.StoppedEventArgs>(playBackStopped);
                waveOut2.Dispose();
                waveOut2 = new WaveOut();
                soundDevice = config.soundDevice();
                waveOut2.DeviceNumber = soundDevice;
                waveOut2.Init(mixer);
            }
            if (volumePct != config.VolumePct() || hz != config.Hz())
            {
                volumePct = config.VolumePct();
                hz = config.Hz();
                makeWave();
            }
            try
            {
                waveOut2.Volume = trackBarVolume.Value / 100.0f;
                PlayBeep(config.Hz(), config.Seconds(), volume);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to play sound\n" + ex.Message);
                waveOut2.Dispose();
                try
                {
                    waveOut2 = new WaveOut();
                    soundDevice = config.soundDevice();
                    waveOut2.DeviceNumber = soundDevice;
                    waveOut2.Init(mixer);
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Unable to open sound device\n" + ex2.Message);
                    soundDevice = -1;
                }
            }
        }

        private void Transmit_Click(object sender, EventArgs e)
        {
            transmit();
        }

        private void trackBarVolume_Scroll(object sender, EventArgs e)
        {
            //axWindowsMediaPlayer2.settings.volume = volume = (ushort)trackBarVolume.Value;
            if (waveOut2 != null)
            {
                waveOut2.Volume = trackBarVolume.Value/100.0f;
                label2.Text = waveOut2.Volume*100 + "%";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            config.Show();
            if (config.WindowState == FormWindowState.Minimized)
            {
                config.WindowState = FormWindowState.Normal;
            }
            config.BringToFront();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Width = this.Width;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.Volume = volume;
            Properties.Settings.Default.Save();
            tcpserver.close();
        }

        private void stop()
        {
            timer1.Stop();
            waveOut2.Pause();
            Transmit.Text = "Transmit";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            stop();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (remoteCommand == 't')
            {
                remoteCommand = '?';
                Console.WriteLine("Transmit start");
                transmit();
            }
            else if (remoteCommand == 's')
            {
                //MessageBox.Show("Stopping");
                //Console.WriteLine("Transmit stop");
                remoteCommand = '?';
                stop();
            }
        }
    }

    public class Win32_SoundDevice
    {
        public ushort? Availability;
        public string Caption;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string CreationClassName;
        public string Description;
        public string DeviceID;
        public ushort? DMABufferSize;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public DateTime? InstallDate;
        public uint? LastErrorCode;
        public string Manufacturer;
        public uint? MPU401Address;
        public string Name;
        public string PNPDeviceID;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string ProductName;
        public string Status;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
    }
}
