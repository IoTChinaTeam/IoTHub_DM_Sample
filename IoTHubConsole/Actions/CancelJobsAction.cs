using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class CancelJobsAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            foreach (var jobId in args.Names)
            {
                var response = await client.CancelJobAsync(jobId);

                Console.WriteLine($"Job {jobId} status = {response.Status}");
            }
        }
    }
}
