using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IoTHubConsole
{
    static class TwinExtension
    {
        static public void Set(this Twin twin, Dictionary<string, string> values)
        {
            foreach (var pair in values)
            {
                dynamic value = pair.Value == "null" ? null : pair.Value;

                int intValue;
                if (int.TryParse(pair.Value, out intValue))
                {
                    value = intValue;
                }

                if (pair.Key.StartsWith("tags."))
                {
                    string name = pair.Key.Substring(5);
                    twin.Tags[name] = value;
                }
                else
                {
                    twin.Properties.Desired[pair.Key] = value;
                }
            }
        }
    }

    static class IoTHubHelper
    {
        static public async Task QueryDevicesByCondition(RegistryManager client, string condition)
        {
            var result = client.CreateQuery(condition);
            while (result.HasMoreResults)
            {
                var twins = await result.GetNextAsTwinAsync();

                foreach (var twin in twins)
                {
                    var device = await client.GetDeviceAsync(twin.DeviceId);
                    OutputDevice(twin, device);
                }
            }
        }

        static public async Task QueryDevicesByIds(RegistryManager client, IEnumerable<string> deviceIDs)
        {
            foreach (var deviceId in deviceIDs)
            {
                var twin = await client.GetTwinAsync(deviceId);
                var device = await client.GetDeviceAsync(deviceId);
                OutputDevice(twin, device);
            }
        }

        static public void OutputDevice(Twin twin, Device device = null)
        {
            var defaultColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Device [{twin.DeviceId}]");
            Console.ForegroundColor = defaultColor;

            Console.WriteLine($"Tag: {twin.Tags.ToJson(Formatting.Indented)}");
            Console.WriteLine($"Desired properties: {twin.Properties.Desired.ToJson(Formatting.Indented)}");
            Console.WriteLine($"Reported properties: {twin.Properties.Reported.ToJson(Formatting.Indented)}");

            if (device != null)
            {
                Console.WriteLine($"Primary key: {device.Authentication.SymmetricKey.PrimaryKey}");
                Console.WriteLine($"Secondary key: {device.Authentication.SymmetricKey.SecondaryKey}");

                var builder = IotHubConnectionStringBuilder.Create(Settings.Default.ConnectionString);
                Console.WriteLine($"HostName={builder.HostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}");
            }

            Console.WriteLine();
        }

        static public async Task WaitJob(JobClient client, JobResponse job)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} {job.Status} {job.DeviceJobStatistics?.SucceededCount.ToString() ?? "-"}/{job.DeviceJobStatistics?.DeviceCount.ToString() ?? "-"}");

                if (job.Status == JobStatus.Failed)
                {
                    Console.WriteLine($"Failed. {job.FailureReason}");
                    break;
                }

                if (job.Status == JobStatus.Completed)
                {
                    Console.WriteLine($"Completed. {job.DeviceJobStatistics.SucceededCount} devices succeeded");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
                job = await client.GetJobAsync(job.JobId);
            }
        }
    }
}
