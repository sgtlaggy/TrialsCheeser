using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using SharpPcap;
using SharpPcap.LibPcap;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibPcapLiveDevice Device;
        private readonly Regex PartialIPRegex = new Regex("[0-9.]");

        public MainWindow()
        {
            InitializeComponent();
            GetCaptureDevice();
            HostIP.Focus();
        }

        private void GetCaptureDevice()
        {
            var w = new DevicePickerWindow();
            w.ShowDialog();
            Device = w.SelectedDevice as LibPcapLiveDevice;
            if (Device is null)
            {
                Close();
            }
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
                HostIP.Background = new SolidColorBrush(Colors.LightGreen);
            else
                HostIP.Background = new SolidColorBrush(Colors.LightCoral);
        }
    }
}
