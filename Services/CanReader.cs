using RollingCounterCheck.Services;
using RollingCounterCheck.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace RollingCounterCheck.Services
{
    public class CanReader
    {
        private readonly MainViewModel _vm;
        private readonly AutoResetEvent _rxEvent = new(false);
        private Thread? _thread;
        private bool _running;

        private readonly List<(TPCANMsg, ulong)> _buffer = new();
        private readonly object _lock = new();

        private readonly Stopwatch _busTimer = Stopwatch.StartNew();
        private long _bits;

        private bool _hasMsg;

        public CanReader(MainViewModel vm, Dispatcher dispatcher)
        {
            _vm = vm;

            new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background,
                (_, _) => UpdateGui(), dispatcher).Start();

            new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background,
                (_, _) => _vm.LedVisible = _hasMsg && !_vm.LedVisible, dispatcher).Start();

            new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background,
                (_, _) => _hasMsg = false, dispatcher).Start();
        }

        public void Start(int baud = 500)
        {
            InitPcan(baud);
            _running = true;
            _thread = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();
        }

        public void SetBaudrate(int baud)
        {
            Stop();
            Start(baud);
            _vm.CanBaudrateKbps = baud;
        }

        public void Stop()
        {
            _running = false;
            _rxEvent.Set();
            _thread?.Join();
            PCANBasic.Uninitialize(PCANBasic.PCAN_USBBUS1);
        }

        private void InitPcan(int baud)
        {
            PCANBasic.Initialize(PCANBasic.PCAN_USBBUS1,
                baud switch
                {
                    125 => TPCANBaudrate.PCAN_BAUD_125K,
                    250 => TPCANBaudrate.PCAN_BAUD_250K,
                    500 => TPCANBaudrate.PCAN_BAUD_500K,
                    1000 => TPCANBaudrate.PCAN_BAUD_1M,
                    _ => TPCANBaudrate.PCAN_BAUD_500K
                });

            uint h = (uint)_rxEvent.SafeWaitHandle.DangerousGetHandle().ToInt64();
            PCANBasic.SetValue(PCANBasic.PCAN_USBBUS1,
                TPCANParameter.PCAN_RECEIVE_EVENT, ref h, (uint)IntPtr.Size);
        }

        private static ulong TsUs(TPCANTimestamp t)
            => ((ulong)t.millis_overflow << 32 | t.millis) * 1000UL + t.micros;

        private void ReadLoop()
        {
            while (_running)
            {
                _rxEvent.WaitOne();
                while (PCANBasic.Read(PCANBasic.PCAN_USBBUS1, out var msg, out var ts) ==
                       TPCANStatus.PCAN_ERROR_OK)
                {
                    _bits += 47 + msg.LEN * 8;
                    lock (_lock)
                        _buffer.Add((msg, TsUs(ts)));
                }
            }
        }

        private void UpdateGui()
        {
            List<(TPCANMsg, ulong)> copy;
            lock (_lock) { copy = new(_buffer); _buffer.Clear(); }

            foreach (var (msg, ts) in copy)
            {
                var row = _vm.GetOrCreate(msg.ID);
                row.DLC = msg.LEN;
                row.Payload = string.Join(" ", msg.DATA[..msg.LEN].Select(b => b.ToString("X2")));
                row.Counter++;

                if (row.LastTimestampUs != 0)
                    row.CycleTimeMs = (ts - row.LastTimestampUs) / 1000.0;
                row.LastTimestampUs = ts;

                if (row.RcEnabled)
                {
                    UInt64 rc = RollingCounterDecoder.Decode(
                        msg.DATA, row.RcStartBit, row.RcBitLength, row.RcBigEndian);

                    row.RcValue = rc;
                    //if (row.LastRcValue.HasValue &&
                    //    !RollingCounterDecoder.IsLinear(row.LastRcValue.Value, rc, row.RcBitLength))
                    //    row.RcErrorCount++;

                    row.LastRcValue = 1; // rc;
                }
            }

            if (copy.Count > 0) _hasMsg = true;

            if (_busTimer.ElapsedMilliseconds > 500)
            {
                _vm.BusLoadPercent = _bits == 0 ? 0 :
                    (_bits / _busTimer.Elapsed.TotalSeconds) / 500_000 * 100;
                _bits = 0;
                _busTimer.Restart();
            }
        }
    }
}
