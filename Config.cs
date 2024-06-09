using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CWGen
{
    public partial class Config : Form
    {
        List<string> soundCards = new List<string>();
        int deviceNumber = -1;
        int seconds;
        int volumePct = 100;
        double hz;
        public Config()
        {
            InitializeComponent();
            string device = Properties.Settings.Default.Device;
            comboBox1.SelectedItem = seconds = Properties.Settings.Default.Seconds;
            numericUpDown1.Value = volumePct = Properties.Settings.Default.VolumePct;
            hz = Properties.Settings.Default.Hz;
            textBoxHz.Text = hz.ToString();
            WasapiOut waveOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 300);
            for (int i = 0; i < WaveOut.DeviceCount; ++i)
            {
                WaveOutCapabilities WOC = WaveOut.GetCapabilities(i);
                soundCards.Add(WOC.ProductName);
                comboBox1.Items.Add(WOC.ProductName);
                if (WOC.ProductName.Equals(device))
                {
                    comboBox1.SelectedIndex = i;
                    deviceNumber = i;
                }
            }
            if (deviceNumber < 0)
            {
                deviceNumber = 0;
                MessageBox.Show("Defaulting soundcard to\n" + soundCards[deviceNumber]);
                comboBox1.SelectedIndex = deviceNumber;
            }
        }

        public int soundDevice()
        {
            return deviceNumber;
        }
        public int Seconds()
        {
            return seconds;
        }
        public double Hz()
        {
            return hz;
        }
        public int soundDevice(int newDeviceNumber)
        {
            deviceNumber = newDeviceNumber;
            return deviceNumber;
        }
        public int VolumePct()
        {
            return volumePct;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            deviceNumber = comboBox1.SelectedIndex;
            Properties.Settings.Default.Device = soundCards[deviceNumber];
            Properties.Settings.Default.Save();
        }

        private void Config_Load(object sender, EventArgs e)
        {
            try
            {
                comboBoxSeconds.SelectedIndex = Properties.Settings.Default.Seconds;
            }
            catch
            {
                comboBoxSeconds.Text = Convert.ToString(Properties.Settings.Default.Seconds);
            }
            toolTip1.SetToolTip(comboBoxSeconds, "0=Click Transmit again to stop\n>0 stop after N seconds");
            seconds = Convert.ToInt16(comboBoxSeconds.SelectedItem);
            hz = Properties.Settings.Default.Hz;

        }

        private void Config_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Width = this.Width;
            Properties.Settings.Default.Height = this.Height;
            if (deviceNumber >= 0) {
                Properties.Settings.Default.Device = soundCards[deviceNumber];
            }
            Properties.Settings.Default.Save();

            if (e.CloseReason != CloseReason.UserClosing)
            {
                return;
            }
            Hide();
            e.Cancel = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            seconds = Convert.ToInt16(comboBoxSeconds.Text);
            Properties.Settings.Default.Seconds = seconds;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            volumePct = (int)numericUpDown1.Value;
            Properties.Settings.Default.VolumePct = volumePct;
            Properties.Settings.Default.Save();
        }

        private void textBoxHz_TextChanged(object sender, EventArgs e)
        {
            if (textBoxHz.TextLength < 1) return;
            hz = Convert.ToDouble(textBoxHz.Text);
            Properties.Settings.Default.Hz = hz;
            Properties.Settings.Default.Save();
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void comboBoxSeconds_TextChanged(object sender, EventArgs e)
        {
        }

        private void comboBoxSeconds_Leave(object sender, EventArgs e)
        {
            seconds = Convert.ToInt16(comboBoxSeconds.Text);
            Properties.Settings.Default.Seconds = seconds;
            Properties.Settings.Default.Save();
        }
    }
}
