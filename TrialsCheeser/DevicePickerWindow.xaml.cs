using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SharpPcap;
using SharpPcap.LibPcap;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for DevicePickerWindow.xaml
    /// </summary>
    public partial class DevicePickerWindow : Window
    {
        private CaptureDeviceList Devices;
        public ICaptureDevice SelectedDevice;

        public DevicePickerWindow()
        {
            InitializeComponent();
            RefreshDeviceList();
            DeviceList.Focus();
        }

        private void RefreshDeviceList()
        {
            Devices = CaptureDeviceList.Instance;
            UpdateDeviceList();
            DeviceList.SelectedIndex = 0;
        }

        private void UpdateDeviceList()
        {
            DeviceList.Items.Clear();
            foreach (var device in Devices)
            {
                DeviceList.Items.Add((device as LibPcapLiveDevice).Interface.FriendlyName);
            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceList.SelectedIndex == -1)
                OkButton.IsEnabled = false;
            else
                OkButton.IsEnabled = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDevice = Devices[DeviceList.SelectedIndex];
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                RefreshDeviceList();
        }

        private void RefreshButton_GotFocus(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsDefault = true;
        }

        private void OtherControl_GotFocus(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsDefault = false;
        }
    }
}
