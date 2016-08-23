using System.Windows;

namespace UITest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class App : Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += (sender, args) => MessageBox.Show(args.Exception.InnerException?.Message ?? args.Exception.Message);
        }
    }
}
