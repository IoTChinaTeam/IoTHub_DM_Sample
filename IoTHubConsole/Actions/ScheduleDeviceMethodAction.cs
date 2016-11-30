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

            var startTime = DateTime.UtcNow + TimeSpan.FromSeconds(args.StartOffsetInSeconds);

            var jobId = args.JobId;
            if(string.IsNullOrEmpty(jobId))
            {
                jobId = Guid.NewGuid().ToString();
            }

            JobResponse job = await client.ScheduleDeviceMethodAsync(
                jobId,
                args.QueryCondition,
                method,
                startTime,
                args.TimeoutInSeconds);

            Console.WriteLine($"{job.Type} job {job.JobId} scheduled");
            await IoTHubHelper.WaitJob(client, job);
        }
    }
}
