using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using RollingCounterCheck.ViewModels;

namespace RollingCounterCheck.Services
{
    public class CanReader
    {
        private readonly MainViewModel _vm;

        private Thread? _readThread;
        private volatile bool _running;

        private readonly AutoResetEvent _receiveEvent = new(false);

        private readonly List<(TPCANMsg Msg, ulong TimestampUs)> _buffer = new();
        private readonly object _lock = new();

        private readonly DispatcherTimer _guiTimer;
        private readonly DispatcherTimer _ledTimer;
        private readonly DispatcherTimer _messageTimeoutTimer;

        private readonly Stopwatch _busLoadTimer = Stopwatch.StartNew();
        private long _bitsReceived;
        private bool _ledState;
        private bool _hasRecentMessage;

        public CanReader(MainViewModel vm, Dispatcher dispatcher)
        {
            _vm = vm;

            // GUI Timer 100ms
            _guiTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100),
                                           DispatcherPriority.Background,
                                           (_, _) => UpdateGui(),
                                           dispatcher);
            _guiTimer.Start();

            // LED blink 2Hz
            _ledTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500),
                                            DispatcherPriority.Background,
                                            (_, _) =>
                                            {
                                                if (_hasRecentMessage)
                                                {
                                                    _ledState = !_ledState;
                                                    _vm.LedVisible = _ledState;
                                                }
                                                else
                                                {
                                                    _ledState = false;
                                                    _vm.LedVisible = false;
                                                }
                                            },
                                            dispatcher);
            _ledTimer.Start();

            // Reset Flag nach 1s ohne neue Nachrichten
            _messageTimeoutTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000),
                                                       DispatcherPriority.Background,
                                                       (_, _) => _hasRecentMessage = false,
                                                       dispatcher);
            _messageTimeoutTimer.Start();
        }

        public void Start(int baudrateKbps = 500)
        {
            if (_running) return;

            InitializePcan(baudrateKbps);

            _running = true;
            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "PCAN Receive Thread"
            };
            _readThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _receiveEvent.Set();
            _readThread?.Join();

            PCANBasic.Uninitialize(PCANBasic.PCAN_USBBUS1);
        }

        public void SetBaudrate(int kbps)
        {
            // Stop Thread
            _running = false;
            _receiveEvent.Set();
            _readThread?.Join();

            // Reinitialize hardware
            PCANBasic.Uninitialize(PCANBasic.PCAN_USBBUS1);
            InitializePcan(kbps);

            // Restart Thread
            _running = true;
            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "PCAN Receive Thread"
            };
            _readThread.Start();

            _vm.CanBaudrateKbps = kbps;
        }

        private void InitializePcan(int kbps)
        {
            PCANBasic.Initialize(PCANBasic.PCAN_USBBUS1, BaudrateFromKbps(kbps));

            uint handleValue = (uint)_receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt64();
            var status = PCANBasic.SetValue(
                PCANBasic.PCAN_USBBUS1,
                TPCANParameter.PCAN_RECEIVE_EVENT,
                ref handleValue,
                (uint)IntPtr.Size);

            if (status != TPCANStatus.PCAN_ERROR_OK)
                throw new InvalidOperationException($"PCAN_RECEIVE_EVENT failed: {status}");
        }

        private static TPCANBaudrate BaudrateFromKbps(int kbps) => kbps switch
        {
            125 => TPCANBaudrate.PCAN_BAUD_125K,
            250 => TPCANBaudrate.PCAN_BAUD_250K,
            500 => TPCANBaudrate.PCAN_BAUD_500K,
            1000 => TPCANBaudrate.PCAN_BAUD_1M,
            _ => TPCANBaudrate.PCAN_BAUD_500K
        };

        private static ulong ToMicroseconds(TPCANTimestamp ts)
            => ((ulong)ts.millis_overflow << 32 | ts.millis) * 1000UL + ts.micros;

        private void ReadLoop()
        {
            while (_running)
            {
                _receiveEvent.WaitOne();
                if (!_running) break;

                while (PCANBasic.Read(PCANBasic.PCAN_USBBUS1,
                                       out TPCANMsg msg,
                                       out TPCANTimestamp ts)
                       == TPCANStatus.PCAN_ERROR_OK)
                {
                    ulong timestampUs = ToMicroseconds(ts);
                    _bitsReceived += 47 + msg.LEN * 8;

                    lock (_lock)
                    {
                        _buffer.Add((msg, timestampUs));
                    }
                }
            }
        }

        private void UpdateGui()
        {
            List<(TPCANMsg Msg, ulong TimestampUs)> messages;
            lock (_lock)
            {
                messages = new List<(TPCANMsg, ulong)>(_buffer);
                _buffer.Clear();
            }

            foreach (var (msg, timestampUs) in messages)
            {
                var row = _vm.GetOrCreate(msg.ID);
                row.DLC = msg.LEN;

                var chars = new char[msg.LEN * 3 - 1];
                int idx = 0;
                for (int i = 0; i < msg.LEN; i++)
                {
                    byte b = msg.DATA[i];
                    chars[idx++] = GetHex(b >> 4);
                    chars[idx++] = GetHex(b & 0xF);
                    if (i < msg.LEN - 1) chars[idx++] = ' ';
                }
                row.Payload = new string(chars);

                row.Counter++;
                if (row.LastTimestampUs != 0)
                    row.CycleTimeMs = (timestampUs - row.LastTimestampUs) / 1000.0;
                row.LastTimestampUs = timestampUs;
            }

            if (messages.Count > 0)
            {
                _hasRecentMessage = true;
            }

            UpdateBusLoad();
        }

        private static char GetHex(int val)
            => (char)(val < 10 ? '0' + val : 'A' + (val - 10));

        private void UpdateBusLoad()
        {
            if (_busLoadTimer.ElapsedMilliseconds < 500) return;

            double seconds = _busLoadTimer.Elapsed.TotalSeconds;

            double busLoad = (_bitsReceived > 0)
                ? (_bitsReceived / seconds) / 500_000.0 * 100.0
                : 0.0;

            _vm.BusLoadPercent = busLoad;

            _bitsReceived = 0;
            _busLoadTimer.Restart();
        }
    }
}
