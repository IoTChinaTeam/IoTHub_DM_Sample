using System.Collections.Generic;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class QueryDevicesAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

            if (args.DeviceIds != null)
            {
                await QueryDevicesByIds(client, args.DeviceIds);
            }
            else
            {
                await QueryDevicesByCondition(client, args.QueryCondition ?? "select * from devices");
            }
        }

        static async Task QueryDevicesByCondition(RegistryManager client, string condition)
        {
            var result = client.CreateQuery(condition);
            while (result.HasMoreResults)
            {
                var twins = await result.GetNextAsTwinAsync();

                foreach (var twin in twins)
                {
                    var device = await client.GetDeviceAsync(twin.DeviceId);
                    IoTHubHelper.OutputDevice(twin, device);
                }
            }
        }

        static async Task QueryDevicesByIds(RegistryManager client, IEnumerable<string> deviceIDs)
        {
            foreach (var deviceId in deviceIDs)
            {
                var twin = await client.GetTwinAsync(deviceId);
                var device = await client.GetDeviceAsync(deviceId);
                IoTHubHelper.OutputDevice(twin, device);
            }
        }
    }
}
