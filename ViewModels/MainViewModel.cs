using RollingCounterCheck.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace RollingCounterCheck.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<CanMessageRow> Messages { get; } = new();

        private readonly object _sync = new();

        private double _busLoadPercent;
        public double BusLoadPercent
        {
            get => _busLoadPercent;
            set
            {
                if (_busLoadPercent != value)
                {
                    _busLoadPercent = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(BusLoadPercent)));
                }
            }
        }

        private int _canBaudrateKbps = 500;
        public int CanBaudrateKbps
        {
            get => _canBaudrateKbps;
            set
            {
                if (_canBaudrateKbps != value)
                {
                    _canBaudrateKbps = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(CanBaudrateKbps)));
                }
            }
        }

        private bool _ledVisible;
        public bool LedVisible
        {
            get => _ledVisible;
            set
            {
                if (_ledVisible != value)
                {
                    _ledVisible = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(LedVisible)));
                }
            }
        }

        public MainViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(Messages, _sync);
        }

        public CanMessageRow GetOrCreate(uint canId)
        {
            var row = Messages.FirstOrDefault(r => r.CanId == canId);
            if (row == null)
            {
                row = new CanMessageRow(canId);
                Messages.Add(row);
            }
            return row;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
