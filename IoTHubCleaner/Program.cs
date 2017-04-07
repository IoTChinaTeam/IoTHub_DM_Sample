using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IoTHubCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = ConfigurationManager.AppSettings["DeviceClientEndpoint"];
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            RemoveAllDevices(registryManager).Wait();
        }

        private static async Task RemoveAllDevices(RegistryManager registryManager)
        {
            int count = 0;

            var totalDevicesQuery = registryManager.CreateQuery("SELECT count() AS TotalDevices FROM devices");
            var content = await totalDevicesQuery.GetNextAsJsonAsync();
            if (!content.Any())
            {
                Console.WriteLine("No devices to remove");
                return;
            }

            var totalDevices = Convert.ToInt32(JsonConvert.DeserializeObject<dynamic>(content.Single()).TotalDevices);
            Console.WriteLine($"Found {totalDevices} devices to be removed");

            while (true)
            {
                var devices = await registryManager.GetDevicesAsync(1000);
                if (!devices.Any())
                {
                    break;
                }

                await RemoveDevices(registryManager, devices);
                count += devices.Count();
                Console.WriteLine($"{count} devices removed, {totalDevices - count} device left");
            }
        }

        private static async Task RemoveDevices(RegistryManager registryManager, IEnumerable<Device> devices)
        {
            var stopWatch = Stopwatch.StartNew();
            var count = 0;

            while (devices.Any())
            {
                var batchDevices = devices.Take(100);
                devices = devices.Skip(100);

                var cts = new CancellationTokenSource();
                await registryManager.RemoveDevices2Async(batchDevices, true, cts.Token);
                count += batchDevices.Count();

                Console.WriteLine($"{stopWatch.Elapsed} used to remove {count} devices");
            }
        }
    }
}
