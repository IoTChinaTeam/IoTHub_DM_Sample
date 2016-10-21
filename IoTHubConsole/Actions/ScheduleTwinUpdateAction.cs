using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class ScheduleTwinUpdateAction
    {
        static public async Task Do(CmdArguments args)
        {
            var twin = new Twin();
            twin.ETag = "*";
            twin.Set(args.KVPairs);

            // Workaround
            twin.Tags["dummy"] = string.Empty;

            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            JobResponse job;
            if (args.DeviceIds != null)
            {
                job = await client.ScheduleTwinUpdateAsync(
                    Guid.NewGuid().ToString(),
                    args.DeviceIds,
                    twin,
                    DateTime.UtcNow,
                    args.TimeoutInSeconds);
            }
            else
            {
                job = await client.ScheduleTwinUpdateAsync(
                    Guid.NewGuid().ToString(),
                    args.QueryCondition,
                    twin,
                    DateTime.UtcNow,
                    args.TimeoutInSeconds);
            }

            await IoTHubHelper.WaitJob(client, job);
        }
    }
}
