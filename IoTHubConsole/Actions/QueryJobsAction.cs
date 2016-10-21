using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;

namespace IoTHubConsole.Actions
{
    static class QueryJobsAction
    {
        static public async Task Do(CmdArguments args)
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
