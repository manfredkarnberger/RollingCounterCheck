using System.ComponentModel;

namespace RollingCounterCheck.Models
{
    public class CanMessageRow : INotifyPropertyChanged
    {
        public uint CanId { get; }
        private int _dlc;
        public int DLC { get => _dlc; set { _dlc = value; OnPropertyChanged(nameof(DLC)); } }

        private string _payload = "";
        public string Payload { get => _payload; set { _payload = value; OnPropertyChanged(nameof(Payload)); } }

        private double _cycleTimeMs;
        public double CycleTimeMs { get => _cycleTimeMs; set { _cycleTimeMs = value; OnPropertyChanged(nameof(CycleTimeMs)); } }

        private int _counter;
        public int Counter { get => _counter; set { _counter = value; OnPropertyChanged(nameof(Counter)); } }

        private bool _logToCsv;
        public bool LogToCsv { get => _logToCsv; set { _logToCsv = value; OnPropertyChanged(nameof(LogToCsv)); } }

        public ulong LastTimestampUs;

        public CanMessageRow(uint id)
        {
            CanId = id;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }
}
