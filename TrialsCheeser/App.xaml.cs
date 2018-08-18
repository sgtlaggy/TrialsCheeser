using System.Windows;

namespace TrialsCheeser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Config.Load();
        }
    }
}
