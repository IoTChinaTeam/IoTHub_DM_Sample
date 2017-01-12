using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace IoTHubConsole.Actions
{
    static class AddDeviceAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

            foreach (var deviceId in args.Ids)
            {
                await client.AddDeviceAsync(new Device(deviceId));
                Console.WriteLine($"{deviceId} added");

                if (args.KVPairs != null)
                {
                    var twin = new Twin();
                    twin.Set(args.KVPairs);

                    await client.UpdateTwinAsync(deviceId, twin, "*");
                }
            }

            await IoTHubHelper.QueryDevicesByIds(client, args.Ids);
        }
    }
}