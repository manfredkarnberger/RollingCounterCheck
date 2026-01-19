using System.Windows;
using System.Windows.Controls;
using RollingCounterCheck.Services;
using RollingCounterCheck.ViewModels;

namespace RollingCounterCheck
{
    public partial class MainWindow : Window
    {
        private readonly CanReader _reader;

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            DataContext = vm;

            _reader = new CanReader(vm, Dispatcher);
            _reader.Start(); // Standard 500 kbit/s
        }

        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            _reader.Stop();
            Application.Current.Shutdown();
        }

        private void MenuCanBaud_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string tagStr && int.TryParse(tagStr, out int baud))
            {
                _reader.SetBaudrate(baud);
            }
        }
    }
}
