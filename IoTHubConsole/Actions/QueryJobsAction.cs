using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IoTHubConsole.Actions
{
    static class QueryJobsAction
    {
        static public async Task Do(CmdArguments args)
        {
            if (args.Ids != null)
            {
                var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

                foreach (var jobId in args.Ids)
                {
                    var job = await client.GetJobAsync(jobId);

                    Console.WriteLine(JsonConvert.SerializeObject(job, Formatting.Indented));
                    Console.WriteLine();
                }
            }
            else
            {
                var manager = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

                var query = manager.CreateQuery(args.QueryCondition ?? "select * from devices.jobs");

                while (query.HasMoreResults)
                {
                    var result = await query.GetNextAsJsonAsync();

                    foreach (var job in result)
                    {
                        Console.WriteLine(job);
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
