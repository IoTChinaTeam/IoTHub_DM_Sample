using System;
using System.Threading.Tasks;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IoTHubConsole.Actions
{
    static class QueryJobSummaryAction
    {
        static public async Task Do(CmdArguments args)
        {
            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);
            var condition = args.QueryCondition == null ? null : JsonConvert.DeserializeObject<QueryJobSummaryCondition>(args.QueryCondition);

            var query = client.CreateQuery(condition?.Type, condition?.Status);
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

    class QueryJobSummaryCondition
    {
        public DeviceJobType? Type = null;
        public DeviceJobStatus? Status = null;
    }
}
