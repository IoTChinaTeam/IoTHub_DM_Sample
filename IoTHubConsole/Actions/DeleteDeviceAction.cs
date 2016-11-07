using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class DeleteDeviceAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

            foreach (var deviceId in args.Ids)
            {
                await client.RemoveDeviceAsync(deviceId);
                Console.WriteLine($"{deviceId} deleted");
            }
        }
    }
}
