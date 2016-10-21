using System;
using System.Linq;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class ScheduleDeviceMethodAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            var method = new CloudToDeviceMethod(args.Names.Single());
            method.SetPayloadJson(args.Values.Single());

            JobResponse job;
            if (args.DeviceIds != null)
            {
                job = await client.ScheduleDeviceMethodAsync(
                    Guid.NewGuid().ToString(),
                    args.DeviceIds,
                    method,
                    DateTime.UtcNow,
                    args.TimeoutInSeconds);
            }
            else
            {
                job = await client.ScheduleDeviceMethodAsync(
                    Guid.NewGuid().ToString(),
                    args.QueryCondition,
                    method,
                    DateTime.UtcNow,
                    args.TimeoutInSeconds);
            }

            await IoTHubHelper.WaitJob(client, job);
        }
    }
}
