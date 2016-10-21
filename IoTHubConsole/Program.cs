using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using IoTHubConsole.Actions;

namespace IoTHubConsole
{
    enum Action
    {
        AddDevices,
        DeleteDevices,
        QueryDevices,
        UpdateTwin,
        ScheduleTwinUpdate,
        InvokeMethod,
        ScheduleDeviceMethod,
        QueryJobs,
        CancelJobs,
        SendMessage
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

        [Argument(ArgumentType.AtMostOnce, ShortName = "o", DefaultValue = 0)]
        public int StartOffsetInSeconds;

        [Argument(ArgumentType.AtMostOnce, ShortName = "t", DefaultValue = 3600)]
        public int TimeoutInSeconds;

        [Argument(ArgumentType.AtMostOnce, ShortName = "m")]
        public string C2DMessage;

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
                case Action.AddDevices:
                    await AddDeviceAction.Do(args);
                    break;

                case Action.DeleteDevices:
                    await DeleteDeviceAction.Do(args);
                    break;

                case Action.QueryDevices:
                    await QueryDevicesAction.Do(args);
                    break;

                case Action.UpdateTwin:
                    await UpdateTwinAction.Do(args);
                    break;

                case Action.ScheduleTwinUpdate:
                    await ScheduleTwinUpdateAction.Do(args);
                    break;

                case Action.InvokeMethod:
                    await InvokeMethodAction.Do(args);
                    break;

                case Action.ScheduleDeviceMethod:
                    await ScheduleDeviceMethodAction.Do(args);
                    break;

                case Action.QueryJobs:
                    await QueryJobsAction.Do(args);
                    break;

                case Action.CancelJobs:
                    await CancelJobsAction.Do(args);
                    break;

                case Action.SendMessage:
                    await SendMessageAction.Do(args);
                    break;

                default:
                    throw new ApplicationException($"Unexpected action: {args.Action}");
            }
        }
    }
}
