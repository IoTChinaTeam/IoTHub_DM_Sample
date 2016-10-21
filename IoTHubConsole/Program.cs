using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using IoTHubConsole.Properties;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace IoTHubConsole
{
    enum Action
    {
        AddDevice,
        DeleteDevice,
        QueryDevice,
        UpdateDevice,
        CallMethod,
        ListJobs
    }

#pragma warning disable 649
    class CmdArguments
    {
        [Argument(ArgumentType.Required, ShortName = "a")]
        public Action Action;

        [Argument(ArgumentType.Multiple, ShortName = "d")]
        public string[] DeviceIds;

        [Argument(ArgumentType.AtMostOnce, ShortName = "q")]
        public string QueryCondition;

        [Argument(ArgumentType.Multiple, ShortName = "n", HelpText = "The name of tag, property or method")]
        public string[] Names;

        [Argument(ArgumentType.Multiple, ShortName = "v", HelpText = "The value of tag/property, or parameter in JSON for the method")]
        public string[] Values;

        [Argument(ArgumentType.AtMostOnce, ShortName = "t", DefaultValue = 3600)]
        public int TimeoutInSeconds;

        public Dictionary<string, string> KVPairs
        {
            get
            {
                if (Names == null || Values == null)
                {
                    return null;
                }

                return Names.
                    Zip(Values, (name, value) => new KeyValuePair<string, string>(name, value)).
                    ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }
    }
#pragma warning restore 649

    class Program
    {
        static void Main(string[] args)
        {
            CmdArguments parsedArgs = new CmdArguments();
            if (!Parser.ParseArgumentsWithUsage(args, parsedArgs))
            {
                return;
            }

            try
            {
                DoAction(parsedArgs).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception raised: {ex}");
            }
        }

        static async Task DoAction(CmdArguments args)
        {
            switch (args.Action)
            {
                case Action.AddDevice:
                    {
                        var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

                        foreach (var deviceId in args.DeviceIds)
                        {
                            await client.AddDeviceAsync(new Device(deviceId));
                            Console.WriteLine($"{deviceId} added");

                            if (args.KVPairs != null)
                            {
                                var twin = new Twin();
                                SetTwin(twin, args.KVPairs);
                                await client.UpdateTwinAsync(deviceId, twin, "*");
                            }
                        }

                        await QueryDevicesByIds(client, args.DeviceIds);
                    }
                    break;

                case Action.DeleteDevice:
                    {
                        var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

                        foreach (var deviceId in args.DeviceIds)
                        {
                            await client.RemoveDeviceAsync(deviceId);
                            Console.WriteLine($"{deviceId} deleted");
                        }
                    }
                    break;

                case Action.QueryDevice:
                    {
                        var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

                        if (args.DeviceIds != null)
                        {
                            await QueryDevicesByIds(client, args.DeviceIds);
                        }
                        else
                        {
                            await QueryDevicesByCondition(client, args.QueryCondition ?? "select * from devices");
                        }
                    }
                    break;

                case Action.UpdateDevice:
                    {
                        if (args.DeviceIds != null && args.DeviceIds.Count() == 1)
                        {
                            var client = RegistryManager.CreateFromConnectionString(Settings.Default.ConnectionString);

                            var deviceId = args.DeviceIds.Single();

                            var twin = await client.GetTwinAsync(deviceId);
                            SetTwin(twin, args.KVPairs);
                            await client.UpdateTwinAsync(deviceId, twin, twin.ETag);
                            Console.WriteLine("1 device updated");
                        }
                        else
                        {
                            var twin = new Twin();
                            SetTwin(twin, args.KVPairs);

                            // Workaround
                            twin.Tags["dummy"] = string.Empty;

                            await BatchUpdateTwin(args, twin);
                        }
                    }
                    break;

                case Action.CallMethod:
                    {
                        var method = new CloudToDeviceMethod(args.Names.Single());
                        method.SetPayloadJson(args.Values.Single());

                        if (args.DeviceIds != null && args.DeviceIds.Count() == 1)
                        {
                            var client = ServiceClient.CreateFromConnectionString(Settings.Default.ConnectionString);

                            var deviceId = args.DeviceIds.Single();

                            var result = await client.InvokeDeviceMethodAsync(deviceId, method);

                            Console.WriteLine($"1 device invoked. Status: {result.Status}, Return: {result.GetPayloadAsJson()}");
                        }
                        else
                        {
                            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

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

                            await WaitJob(client, job);
                        }
                    }
                    break;

                case Action.ListJobs:
                    {
                        var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

                        var query = client.CreateQuery();
                        while (query.HasMoreResults)
                        {
                            var result = await query.GetNextAsJobResponseAsync();

                            foreach (var job in result.OrderBy(j => j.StartTimeUtc))
                            {
                                Console.WriteLine($"Job [{job.JobId}]");
                                Console.WriteLine($"  type: {job.Type}");
                                Console.WriteLine($"  start: {job.StartTimeUtc}");
                                Console.WriteLine($"  status: {job.Status}");
                                Console.WriteLine($"  progress: {job.DeviceJobStatistics?.SucceededCount.ToString() ?? "-"}/{job.DeviceJobStatistics?.DeviceCount.ToString() ?? "-"}");
                                if (!string.IsNullOrWhiteSpace(job.FailureReason))
                                {
                                    Console.WriteLine($"  error: {job.FailureReason}");
                                }

                                Console.WriteLine();
                            }
                        }
                    }
                    break;

                default:
                    throw new ApplicationException($"Unexpected action: {args.Action}");
            }
        }

        static string FormatJson(string text)
        {
            var obj = JsonConvert.DeserializeObject(text);
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        static async Task QueryDevicesByCondition(RegistryManager client, string condition)
        {
            var result = client.CreateQuery(condition);
            while (result.HasMoreResults)
            {
                var twins = await result.GetNextAsTwinAsync();

                foreach (var twin in twins)
                {
                    var device = await client.GetDeviceAsync(twin.DeviceId);
                    OutputDevice(twin, device);
                }
            }
        }

        static async Task QueryDevicesByIds(RegistryManager client, IEnumerable<string> deviceIDs)
        {
            foreach (var deviceId in deviceIDs)
            {
                var twin = await client.GetTwinAsync(deviceId);
                var device = await client.GetDeviceAsync(deviceId);
                OutputDevice(twin, device);
            }
        }

        static void OutputDevice(Twin twin, Device device)
        {
            Console.WriteLine($"Device [{twin.DeviceId}]\r\n");
            Console.WriteLine($"Tag: {twin.Tags.ToJson(Formatting.Indented)}\r\n");
            Console.WriteLine($"Desired properties: {twin.Properties.Desired.ToJson(Formatting.Indented)}\r\n");
            Console.WriteLine($"Reported properties: {twin.Properties.Reported.ToJson(Formatting.Indented)}\r\n");

            Console.WriteLine($"Primary key: {device.Authentication.SymmetricKey.PrimaryKey}");
            Console.WriteLine($"Secondary key: {device.Authentication.SymmetricKey.SecondaryKey}");

            Console.WriteLine();
            Console.WriteLine("------");
            Console.WriteLine();
        }

        static async Task BatchUpdateTwin(CmdArguments args, Twin twin)
        {
            var client = JobClient.CreateFromConnectionString(Settings.Default.ConnectionString);

            twin.ETag = "*";

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

            await WaitJob(client, job);
        }

        static async Task WaitJob(JobClient client, JobResponse job)
        {
            while (true)
            {
                Console.WriteLine($"{job.Status} {job.DeviceJobStatistics?.SucceededCount.ToString() ?? "-"}/{job.DeviceJobStatistics?.DeviceCount.ToString() ?? "-"}");

                if (job.Status == JobStatus.Failed)
                {
                    Console.WriteLine($"Failed. {job.FailureReason}");
                    break;
                }

                if (job.Status == JobStatus.Completed)
                {
                    Console.WriteLine($"Completed. {job.DeviceJobStatistics.SucceededCount} devices succeeded");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
                job = await client.GetJobAsync(job.JobId);
            }
        }

        static void SetTwin(Twin twin, Dictionary<string, string> values)
        {
            foreach (var pair in values)
            {
                if (pair.Key.StartsWith("tags."))
                {
                    string name = pair.Key.Substring(5);
                    twin.Tags[name] = pair.Value;
                }
                else
                {
                    twin.Properties.Desired[pair.Key] = pair.Value;
                }
            }
        }
    }
}
