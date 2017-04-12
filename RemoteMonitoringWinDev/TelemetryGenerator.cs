using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.WinDev
{
    class TelemetryGenerator : IDisposable
    {
        private string deviceId;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter networkInCounter;
        private PerformanceCounter networkOutCounter;

        private double cpuValue;
        private double networkInValue;
        private double networkOutValue;

        public TelemetryGenerator(string deviceId)
        {
            this.deviceId = deviceId;

            var category = new PerformanceCounterCategory("Network Interface");
            var networkCard = category.GetInstanceNames().First(s => !s.StartsWith("isatap"));

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            networkInCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard);
            networkOutCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard);

            cpuValue = double.NaN;
            networkInValue = double.NaN;
            networkOutValue = double.NaN;
        }

        public object GetNext()
        {
            cpuValue = TimeSlideAvg(cpuValue, cpuCounter.NextValue());
            networkInValue = TimeSlideAvg(networkInValue, networkInCounter.NextValue());
            networkOutValue = TimeSlideAvg(networkOutValue, networkOutCounter.NextValue());

            return new
            {
                DeviceId = deviceId,
                CPUUsage = cpuValue,
                NetworkIn = networkInValue * 8 / 1000000,
                NetworkOut = networkOutValue * 8 / 1000000,
            };
        }

        private double TimeSlideAvg(double oldValue, double newValue, double weight = 15)
        {
            if (double.IsNaN(oldValue))
            {
                return newValue;
            }
            else
            {
                return (oldValue * weight + newValue) / (weight + 1);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cpuCounter.Dispose();
                    networkInCounter.Dispose();
                    networkOutCounter.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
