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
                await IoTHubHelper.QueryDevicesByIds(client, args.DeviceIds);
            }
            else
            {
                await IoTHubHelper.QueryDevicesByCondition(client, args.QueryCondition ?? "select * from devices");
            }
        }
    }
}
