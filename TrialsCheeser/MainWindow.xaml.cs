using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Timers;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibPcapLiveDevice Device;
        private readonly Regex PartialIPRegex = new Regex("[0-9.]");
        private Timer PacketTimer = new Timer(500);
        private int PacketCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            HostIP.Focus();
            GetCaptureDevice();
            if (Device != null)
            {
                Device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
                Device.Open(DeviceMode.Promiscuous, 1000);
                Device.Filter = "ip and udp and host 0.0.0.0";
                Device.StartCapture();
                PacketTimer.Elapsed += async (sender, e) => Timer_Elapsed();
                PacketTimer.Start();
            }
        }

        private void UpdateMatchNotifier()
        {
            Brush brush;
            string text;
            if (PacketCount == 0)
            {
                brush = Brushes.Red;
                text = "Not matched.";
            }
            else if (PacketCount <= 5)
            {
                brush = Brushes.Orange;
                text = "Checking...";
            }
            else
            {
                brush = Brushes.Lime;
                text = "Matched!";
            }
            MatchCircle.Dispatcher.BeginInvoke((Action)(() => MatchCircle.Fill = brush));
            MatchLabel.Dispatcher.BeginInvoke((Action)(() => MatchLabel.Content = text));
        }

        private void Timer_Elapsed()
        {
            UpdateMatchNotifier();
            PacketCount = 0;
        }

        private void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            PacketCount += 1;
        }

        private void GetCaptureDevice()
        {
            var w = new DevicePickerWindow();
            w.ShowDialog();
            if (w.SelectedDevice == null)
                Close();
            else
                Device = w.SelectedDevice as LibPcapLiveDevice;
        }

        private void DevicesButton_Click(object sender, RoutedEventArgs e)
        {
            GetCaptureDevice();
        }

        private bool ValidateInput(string ip)
        {
            return PartialIPRegex.IsMatch(ip);
        }

        private void HostIP_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!ValidateInput(text))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void HostIP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ValidateInput(e.Text);
        }

        private void HostIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = HostIP.Text;
            if (IPAddress.TryParse(text, out IPAddress ip) && text.Split(new[] { '.' }, StringSplitOptions.None).Length == 4)
            {
                HostIP.Background = Brushes.LightGreen;
                Device.Filter = $"ip and udp and host {text}";
            }
            else
            {
                HostIP.Background = Brushes.LightCoral;
                Device.Filter = "ip and udp and host 0.0.0.0";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Device != null)
            {
                if (PacketTimer.Enabled)
                    PacketTimer.Stop();
                if (Device.Started)
                    Device.StopCapture();
                if (Device.Opened)
                    Device.Close();
            }
        }
    }
}
