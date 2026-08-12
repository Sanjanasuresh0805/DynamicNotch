using System;
using System.Windows.Threading;

namespace DynamicNotch.Services
{
    public class BatteryEventArgs : EventArgs
    {
        public int Percentage { get; set; }
        public bool IsCharging { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class BatteryService
    {
        private DispatcherTimer? _timer;
        public event EventHandler<BatteryEventArgs>? BatteryUpdated;

        public void Start()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += (s, e) => Update();
            _timer.Start();
            Update();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer = null;
        }

        private void Update()
        {
            try
            {
                var power = System.Windows.Forms.SystemInformation.PowerStatus;
                bool available = power.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery
                                 && power.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.Unknown;
                int percent = (int)Math.Round(power.BatteryLifePercent * 100);
                bool charging = power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

                BatteryUpdated?.Invoke(this, new BatteryEventArgs
                {
                    Percentage = percent,
                    IsCharging = charging,
                    IsAvailable = available
                });
            }
            catch
            {
                BatteryUpdated?.Invoke(this, new BatteryEventArgs
                {
                    Percentage = 0,
                    IsCharging = false,
                    IsAvailable = false
                });
            }
        }
    }
}