using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibPcapLiveDevice Device;
        private readonly Regex PartialIPPattern = new Regex("[0-9.]");
        private Timer PacketTimer = new Timer(500);
        private int PacketCount = 0;
        private WebClient WebClient = new WebClient();

        public MainWindow()
        {
            InitializeComponent();
            HostIPTextBox.Focus();
            GetCaptureDevice();
            if (Device != null)
            {
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
            Dispatcher.Invoke(() =>
            {
                MatchCircle.Fill = brush;
                MatchLabel.Content = text;
            });
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
            if (Device != null)
            {
                if (Device.Started)
                    Device.StopCapture();
                if (Device.Opened)
                    Device.Close();
            }
            var w = new DevicePickerWindow();
            w.ShowDialog();
            if (w.SelectedDevice == null && Device == null)
            {
                Close();
            }
            else
            {
                Device = (w.SelectedDevice != null) ? w.SelectedDevice as LibPcapLiveDevice : Device;
                Device.OnPacketArrival -= OnPacketArrival;
                Device.OnPacketArrival += OnPacketArrival;
                Device.Open(DeviceMode.Promiscuous, 1000);
                SetDeviceFilter();
                Device.StartCapture();
            }
        }

        private void DevicesButton_FocusChanged(object sender, RoutedEventArgs e)
        {
            DevicesButton.IsDefault = !DevicesButton.IsDefault;
        }

        private void DevicesButton_Click(object sender, RoutedEventArgs e)
        {
            GetCaptureDevice();
        }

        private void SetDeviceFilter()
        {
            var text = HostIPTextBox.Text;
            if (IPAddress.TryParse(text, out IPAddress ip) && text.Split(new[] { '.' }, StringSplitOptions.None).Length == 4)
            {
                HostIPTextBox.Background = Brushes.LightGreen;
                Device.Filter = $"ip and udp and host {ip.ToString()}";
            }
            else
            {
                HostIPTextBox.Background = Brushes.LightCoral;
                Device.Filter = "ip and udp and host 0.0.0.0";
            }
        }

        private bool ValidateInput(string ip)
        {
            return PartialIPPattern.IsMatch(ip);
        }

        private void HostIPTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!ValidateInput(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void HostIPTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ValidateInput(e.Text);
        }

        private void HostIPTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetDeviceFilter();
        }

        private void CopyIPButton_FocusChanged(object sender, RoutedEventArgs e)
        {
            CopyIPButton.IsDefault = !CopyIPButton.IsDefault;
        }

        private async void CopyIPButton_Click(object sender, RoutedEventArgs e)
        {
            CopyIPButton.IsEnabled = false;
            try
            {
                string response = (await WebClient.DownloadStringTaskAsync("https://ipinfo.io/ip")).Trim(); // BUG: Sometimes blocks UI when host unreachable.
                if (IPAddress.TryParse(response, out IPAddress ip))
                {
                    Clipboard.SetText(ip.ToString());
                    CopyIPButton.Content = "Copied";
                }
                else
                {
                    CopyIPButton.Content = "Error";
                }
            }
            catch (WebException)
            {
                CopyIPButton.Content = "Error";
            }
            await Task.Delay(2000);
            CopyIPButton.Content = "Copy My IP";
            CopyIPButton.IsEnabled = true;
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
