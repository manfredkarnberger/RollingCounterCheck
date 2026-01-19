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
            _reader.Start();
        }

        private void Exit_Click(object s, RoutedEventArgs e)
        {
            _reader.Stop();
            Application.Current.Shutdown();
        }

        private void Baud_Click(object s, RoutedEventArgs e)
        {
            if (s is MenuItem m && int.TryParse(m.Tag.ToString(), out int b))
                _reader.SetBaudrate(b);
        }
    }
}
