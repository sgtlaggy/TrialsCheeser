using Extensions;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibPcapLiveDevice Device;
        private readonly Regex NonIPPattern = new Regex("[^0-9.]");
        private Timer PacketTimer = new Timer(1000);
        private int PacketCount = 0;
        private int MatchThreshold;
        private HttpClient HttpClient = new HttpClient();
        private string LastIPFilePath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\TrialsCheeser_LastIP.txt");

        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            var ontop = Config.Get("lastSession/onTop");
            OnTopCheck.IsChecked = string.IsNullOrEmpty(ontop) ? false : bool.Parse(ontop);
            HostIPTextBox.Text = Config.Get("lastSession/ip");
            if (!int.TryParse(Config.Get("lastSession/threshold"), out MatchThreshold))
                MatchThreshold = 5;
            ThresholdTextBox.Text = MatchThreshold.ToString();
            try
            {
                GetCaptureDevice(true);
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
                PacketTimer.Elapsed += Timer_Elapsed;
                PacketTimer.Start();
            }
        }

        private void UpdateMatchNotifier()
        {
            Brush brush;
            string text;
            if (PacketCount == 0 || Device.Filter == null)
            {
                brush = Brushes.Red;
                text = "Not matched.";
            }
            else if (PacketCount <= MatchThreshold)
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

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateMatchNotifier();
            PacketCount = 0;
        }

        private void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            PacketCount += 1;
        }

        private void GetCaptureDevice(bool firstOpen = false)
        {
            if (Device != null)
                Device.StopAndClose();
            LibPcapLiveDevice selectedDevice = null;
            if (firstOpen)
            {
                var lastDeviceName = Config.Get("lastSession/deviceName");
                var devices = CaptureDeviceList.Instance;
                foreach (var device in devices)
                {
                    if (device.Name == lastDeviceName)
                    {
                        selectedDevice = device as LibPcapLiveDevice;
                    }
                }
            }
            if (selectedDevice == null)
            {
                var w = new DevicePickerWindow();
                w.ShowDialog();
                selectedDevice = w.SelectedDevice as LibPcapLiveDevice;
            }
            if (selectedDevice == null && Device == null)
            {
                Close();
            }
            else
            {
                Device = selectedDevice ?? Device;
                Device.OnPacketArrival -= OnPacketArrival;
                Device.OnPacketArrival += OnPacketArrival;
                Device.Open(DeviceMode.Promiscuous, 1000);
                SetDeviceFilter();
                Device.StartCapture();
            }
        }

        private void SetDeviceFilter()
        {
            var text = HostIPTextBox.Text;
            if (IPAddress.TryParse(text, out IPAddress ip) && text.Split(new[] { '.' }, StringSplitOptions.None).Length == 4)
            {
                HostIPTextBox.Background = Brushes.LightGreen;
                Device.Filter = $"ip and udp and host {ip.ToString()}";
                PacketCount = 0;
                PacketTimer.Start();
            }
            else
            {
                HostIPTextBox.Background = Brushes.LightCoral;
                Device.Filter = null;
                PacketTimer.Stop();
            }
        }

        private void HostIPTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var clip = (string)e.DataObject.GetData(typeof(string));
                clip = NonIPPattern.Replace(clip, string.Empty);
                var currentLength = HostIPTextBox.Text.Length;
                var selectionLength = HostIPTextBox.SelectionLength;
                if (currentLength == HostIPTextBox.MaxLength)
                {
                    e.CancelCommand();
                    return;
                }
                else if (currentLength - selectionLength + clip.Length > HostIPTextBox.MaxLength)
                {
                    clip = clip.Substring(0, HostIPTextBox.MaxLength - currentLength + selectionLength);
                }
                var text = HostIPTextBox.Text;
                var start = HostIPTextBox.SelectionStart;
                var caret = HostIPTextBox.CaretIndex;
                var newText = text.Substring(0, start) + clip + text.Substring(start + selectionLength);
                HostIPTextBox.Text = newText;
                HostIPTextBox.CaretIndex = caret + clip.Length;
            }
            e.CancelCommand();
        }

        private void HostIPTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private void HostIPTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = NonIPPattern.IsMatch(e.Text);
        }

        private void HostIPTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var caret = HostIPTextBox.CaretIndex;
            HostIPTextBox.Text = HostIPTextBox.Text.Replace(" ", string.Empty);
            HostIPTextBox.CaretIndex = caret;
            if (Device != null)
                SetDeviceFilter();
            Config.Set("lastSession/ip", HostIPTextBox.Text);
        }

        private void ChangeThreshold(int changeBy)
        {
            int value = MatchThreshold + changeBy;
            ThresholdTextBox.Text = value.Clamp(0, 99).ToString();
        }

        private void ThresholdTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ChangeThreshold(e.Delta.Clamp(-1, 1));
        }

        private void ThresholdTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                ChangeThreshold(1);
            else if (e.Key == Key.Down)
                ChangeThreshold(-1);
        }

        private void ThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsInteger();
        }

        private void ThresholdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value = int.Parse(ThresholdTextBox.Text);
            MatchThreshold = value;
            ThresholdTextBox.Text = value.ToString();
            Config.Set("lastSession/threshold", ThresholdTextBox.Text);
        }

        private void ThresholdTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        private void Button_FocusChanged(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsDefault = btn.IsFocused;
        }

        private void DevicesButton_Click(object sender, RoutedEventArgs e)
        {
            GetCaptureDevice();
        }

        private void OnTopCheck_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Config.Set("lastSession/onTop", OnTopCheck.IsChecked.ToString());
            Topmost = OnTopCheck.IsChecked == null ? false : (bool)OnTopCheck.IsChecked;
        }

        private async void CopyIPButton_Click(object sender, RoutedEventArgs e)
        {
            CopyIPButton.IsEnabled = false;
            CopyIPButton.Content = "...";
            string status;
            try
            {
                string response = await HttpClient.GetStringAsync("https://api.ipify.org/");
                if (IPAddress.TryParse(response, out IPAddress ip))
                {
                    Clipboard.SetText(ip.ToString());
                    status = "Copied";
                }
                else
                {
                    status = "Parse Error";
                }
            }
            catch (HttpRequestException)
            {
                status = "Fetch Error";
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
                Device.StopAndClose();
        }
    }
}
