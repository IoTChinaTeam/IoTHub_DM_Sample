using System;
using System.Linq;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class InvokeMethodAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = ServiceClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            var deviceId = args.DeviceIds.Single();
            var method = new CloudToDeviceMethod(args.Names.Single());
            method.SetPayloadJson(args.Values.Single());

            var result = await client.InvokeDeviceMethodAsync(deviceId, method);

            Console.WriteLine($"1 device invoked. Status: {result.Status}, Return: {result.GetPayloadAsJson()}");
        }
    }
}
