using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IoTHubReaderSample
{
    class Indicators
    {
        private const int deviceToIoTHubDelayWeight = 15;
        private const int e2eDelayWeight = 15;

        public int TotalDevices
        {
            get
            {
                lock (devices)
                {
                    return devices.Count;
                }
            }
        }

        public int TotalMessages { get; private set; }
        public double AvgDeviceToIoTHubDelay { get; private set; }
        public double AvgE2EDelay { get; private set; }
        public string SampleEvent { get; private set; }

        private HashSet<string> devices = new HashSet<string>();

        public void Reset()
        {
            devices.Clear();
            TotalMessages = 0;
            AvgDeviceToIoTHubDelay = double.NaN;
            AvgE2EDelay = double.NaN;
            SampleEvent = string.Empty;
        }

        public void Push(EventData eventData, string interestringDevice)
        {
            var receiveTimeUtc = DateTime.UtcNow;
            TotalMessages++;

            var deviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
            if (!devices.Contains(deviceId))
            {
                lock (devices)
                {
                    devices.Add(deviceId);
                }
            }

            var bytes = eventData.GetBytes();
            var content = Encoding.UTF8.GetString(bytes);
            if (interestringDevice == null || interestringDevice == deviceId)
            {
                SampleEvent = content;
            }

            var root = JsonConvert.DeserializeObject(content) as JToken;
            var sendTimeUtc = root.Value<DateTime>("DeviceUtcDatetime");
            if (sendTimeUtc > DateTime.MinValue)
            {
                var enqueueTimeUtc = eventData.EnqueuedTimeUtc;

                var deviceToIoTHubDelay = (enqueueTimeUtc - sendTimeUtc).TotalMilliseconds;
                var e2eDelay = (receiveTimeUtc - sendTimeUtc).TotalMilliseconds;

                if (double.IsNaN(AvgDeviceToIoTHubDelay))
                {
                    AvgDeviceToIoTHubDelay = deviceToIoTHubDelay;
                }
                else
                {
                    AvgDeviceToIoTHubDelay = (AvgDeviceToIoTHubDelay * deviceToIoTHubDelayWeight + deviceToIoTHubDelay) / (deviceToIoTHubDelayWeight + 1);
                }

                if (double.IsNaN(AvgE2EDelay))
                {
                    AvgE2EDelay = e2eDelay;
                }
                else
                {
                    AvgE2EDelay = (AvgE2EDelay * e2eDelayWeight + e2eDelay) / (e2eDelayWeight + 1);
                }
            }
        }
    }
}
