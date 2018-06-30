using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
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
        private Timer PacketTimer = new Timer(1000);
        private int PacketCount = 0;
        private HttpClient HttpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            HostIPTextBox.Focus();
            try
            {
                GetCaptureDevice();
            }
            catch (DllNotFoundException)
            {
                MessageBoxResult result = MessageBox.Show("You must have WinPcap installed to use this program.\nWould you like to go to its website now?", "WinPcap Not Installed", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start("https://www.winpcap.org/install/default.htm");
                Application.Current.Shutdown();
            }
            if (Device != null)
            {
                PacketTimer.Elapsed += (sender, e) => Timer_Elapsed();
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
            CopyIPButton.Content = "...";
            string status;
            try
            {
                string response = (await HttpClient.GetStringAsync("https://ipinfo.io/ip")).Trim();
                if (IPAddress.TryParse(response, out IPAddress ip))
                {
                    Clipboard.SetText(ip.ToString());
                    status = "Copied";
                }
                else
                {
                    status = "Error";
                }
            }
            catch (HttpRequestException)
            {
                status = "Error";
            }
            CopyIPButton.Content = status;
            await Task.Delay(2000);
            CopyIPButton.Content = "Copy My IP";
            CopyIPButton.IsEnabled = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            PacketTimer.Enabled = false;
            if (Device != null)
            {
                if (Device.Started)
                    Device.StopCapture();
                if (Device.Opened)
                    Device.Close();
            }
        }
    }
}
