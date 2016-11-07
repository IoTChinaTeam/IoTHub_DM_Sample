using System;
using System.Text;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class SendMessageAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = ServiceClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            var message = new Message(Encoding.UTF8.GetBytes(args.C2DMessage));

            foreach (var deviceId in args.Ids)
            {
                await client.SendAsync(deviceId, message);

                Console.WriteLine($"C2D message {args.C2DMessage} sent to device {deviceId}");
            }
        }
    }
}
