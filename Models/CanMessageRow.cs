using System.ComponentModel;

namespace RollingCounterCheck.Models
{
    public class CanMessageRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public uint CanId { get; }

        public CanMessageRow(uint canId)
        {
            CanId = canId;
        }

        // ---------------------------
        // CAN Basisdaten
        // ---------------------------

        private int _dlc;
        public int DLC
        {
            get => _dlc;
            set
            {
                if (_dlc != value)
                {
                    _dlc = value;
                    Notify(nameof(DLC));
                }
            }
        }

        private string _payload = string.Empty;
        public string Payload
        {
            get => _payload;
            set
            {
                if (_payload != value)
                {
                    _payload = value;
                    Notify(nameof(Payload));
                }
            }
        }

        private double _cycleTimeMs;
        public double CycleTimeMs
        {
            get => _cycleTimeMs;
            set
            {
                if (_cycleTimeMs != value)
                {
                    _cycleTimeMs = value;
                    Notify(nameof(CycleTimeMs));
                }
            }
        }

        private ulong _counter;
        public ulong Counter
        {
            get => _counter;
            set
            {
                if (_counter != value)
                {
                    _counter = value;
                    Notify(nameof(Counter));
                }
            }
        }

        public ulong LastTimestampUs { get; set; }

        private bool _logToCsv;
        public bool LogToCsv
        {
            get => _logToCsv;
            set
            {
                if (_logToCsv != value)
                {
                    _logToCsv = value;
                    Notify(nameof(LogToCsv));
                }
            }
        }

        // ---------------------------
        // Rolling Counter Konfiguration
        // ---------------------------

        private bool _rcEnabled;
        public bool RcEnabled
        {
            get => _rcEnabled;
            set
            {
                if (_rcEnabled != value)
                {
                    _rcEnabled = value;
                    Notify(nameof(RcEnabled));
                }
            }
        }

        private int _rcStartBit;
        public int RcStartBit
        {
            get => _rcStartBit;
            set
            {
                if (_rcStartBit != value)
                {
                    _rcStartBit = value;
                    Notify(nameof(RcStartBit));
                }
            }
        }

        private int _rcBitLength = 4;
        public int RcBitLength
        {
            get => _rcBitLength;
            set
            {
                if (_rcBitLength != value)
                {
                    _rcBitLength = value;
                    Notify(nameof(RcBitLength));
                }
            }
        }

        private bool _rcBigEndian;
        public bool RcBigEndian
        {
            get => _rcBigEndian;
            set
            {
                if (_rcBigEndian != value)
                {
                    _rcBigEndian = value;
                    Notify(nameof(RcBigEndian));
                }
            }
        }

        private UInt64? _rcValue;
        public UInt64? RcValue
        {
            get => _rcValue;
            set
            {
                if (_rcValue != value)
                {
                    _rcValue = value;
                    Notify(nameof(RcValue));
                }
            }
        }
        // ---------------------------
        // Rolling Counter Status
        // ---------------------------

        public int? LastRcValue { get; set; }

        private ulong _rcErrorCount;
        public ulong RcErrorCount
        {
            get => _rcErrorCount;
            set
            {
                if (_rcErrorCount != value)
                {
                    _rcErrorCount = value;
                    Notify(nameof(RcErrorCount));
                }
            }
        }

        // ---------------------------
        // Helper
        // ---------------------------

        private void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
