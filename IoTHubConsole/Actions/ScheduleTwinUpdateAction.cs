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

            var startTime = DateTime.UtcNow + TimeSpan.FromSeconds(args.StartOffsetInSeconds);

            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            JobResponse job = await client.ScheduleTwinUpdateAsync(
                Guid.NewGuid().ToString(),
                args.QueryCondition,
                twin,
                startTime,
                args.TimeoutInSeconds);

            Console.WriteLine($"{job.Type} job {job.JobId} scheduled");
            await IoTHubHelper.WaitJob(client, job);
        }
    }
}
