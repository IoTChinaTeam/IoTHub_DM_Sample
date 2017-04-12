using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.WinDev
{
    class Device
    {
        private string connectionString;
        private string deviceId;

        private Task mainTask;
        private CancellationTokenSource cancellationTokenSource;

        public Device(string connectionString)
        {
            this.connectionString = connectionString;

            var builder = IotHubConnectionStringBuilder.Create(connectionString);
            deviceId = builder.DeviceId;
        }

        public void Run()
        {
            if (mainTask != null)
            {
                throw new ApplicationException("Device is already running");
            }

            cancellationTokenSource = new CancellationTokenSource();
            mainTask = RunLoop(cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (mainTask == null)
            {
                throw new ApplicationException("Device is already stopped");
            }

            cancellationTokenSource.Cancel();
            mainTask.Wait();
            cancellationTokenSource.Dispose();
        }

        private async Task RunLoop(CancellationToken ct)
        {
            using (var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt))
            using (var telemetryGenerator = new TelemetryGenerator(deviceId))
            {
                await deviceClient.SetMethodHandlerAsync("InitiateFirmwareUpdate", OnInitiateFirmwareUpdate, null);

                await SendDeviceInfoAsnc(deviceClient);

                while (!ct.IsCancellationRequested)
                {
                    var telemetry = telemetryGenerator.GetNext();
                    await SendAsync(deviceClient, telemetry);

                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }

        private async Task SendDeviceInfoAsnc(DeviceClient deviceClient)
        {
            var deviceInfo = new
            {
                DeviceProperties = new
                {
                    DeviceID = deviceId,
                    HubEnabledState = true
                },
                Commands = new[]
                {
                    new
                    {
                        Name = "InitiateFirmwareUpdate",
                        DeliveryType = 1,
                        Parameters = new[]
                        {
                            new
                            {
                                Name = "FwPackageUri",
                                Type = "string"
                            }
                        },
                        Description = "Updates device Firmware. Use parameter FwPackageUri to specify the URI of the firmware file, e.g. https://iotrmassets.blob.core.windows.net/firmwares/FW20.bin"
                    }
                },
                Telemetry = new[]
                {
                    new
                    {
                        Name = "CPUUsage",
                        DisplayName = "CPU usage (%)",
                        Type = "double"
                    },
                    new
                    {
                        Name = "NetworkIn",
                        DisplayName = "Network input (Mbps)",
                        Type = "double"
                    },
                    new
                    {
                        Name = "NetworkOut",
                        DisplayName = "Network output (Mbps)",
                        Type = "double"
                    }
                },
                Version = "1.0",
                ObjectType = "DeviceInfo"
            };

            await SendAsync(deviceClient, deviceInfo);
        }

        private async Task SendAsync(DeviceClient deviceClient, object eventData)
        {
            var content = JsonConvert.SerializeObject(eventData, Formatting.Indented);
            Console.WriteLine($"Sending {content}");

            var bytes = Encoding.UTF8.GetBytes(content);
            var message = new Message(bytes);
            message.Properties["EventId"] = Guid.NewGuid().ToString();

            await deviceClient.SendEventAsync(message);
        }

        private async Task<MethodResponse> OnInitiateFirmwareUpdate(MethodRequest methodRequest, object userContext)
        {
            // Not implemented yet

            return await Task.FromResult(new MethodResponse(200));
        }
    }
}
