using SharpPcap.LibPcap;

namespace Extensions
{
    static class Extensions
    {
        public static bool IsInteger(this string str)
        {
            return int.TryParse(str, out int _);
        }

        public static int Clamp(this int i, int min, int max)
        {
            if (i < min)
                return min;
            else if (i > max)
                return max;
            else
                return i;
        }

        public static void StopAndClose(this LibPcapLiveDevice device)
        {
            if (device.Started)
                device.StopCapture();
            if (device.Opened)
                device.Close();
        }
    }
}