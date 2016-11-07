using System;
using System.Linq;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class UpdateTwinAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

            var deviceId = args.Ids.Single();

            var twin = new Twin(deviceId);
            twin.Set(args.KVPairs);

            twin = await client.UpdateTwinAsync(deviceId, twin, "*");

            IoTHubHelper.OutputDevice(twin);
            Console.WriteLine("1 device updated");
        }
    }
}
