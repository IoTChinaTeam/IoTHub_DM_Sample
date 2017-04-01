using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using System.Diagnostics;
using System.Net;

namespace DeviceLoad
{
    class Setting
    {
        private Setting() { }

        public static Setting Parse(string[] args)
        {
            try
            {
                Setting setting = new Setting();

                if (args.Length < 5)
                {
                    throw new ArgumentException("Not supported format");
                }

                setting.OutputStorageConnectionString = args[0].Trim('"');
                setting.DeviceClientEndpoint = args[1].Trim('"');
                setting.DevicePerVm = int.Parse(args[2]);
                setting.MessagePerMin = int.Parse(args[3]);
                setting.DurationInMin = int.Parse(args[4]);
                setting.BatchJobId = args[5];
                setting.DeviceIdPrefix = args[6].Trim('"');
                if (args.Length > 7)
                {
                    setting.Message = args[7].Trim('"');
                    if (setting.Message.Contains("TestJobType=Blob"))
                    {
                        setting.ReadBlobSwitch = true;
                        setting.Message = setting.Message.Replace(";TestJobType=Blob", "");
                    }
                    else
                        setting.ReadBlobSwitch = false;
                }

                setting.IotHubManager = RegistryManager.CreateFromConnectionString(setting.DeviceClientEndpoint);
                setting.IoTHubHostName = setting.DeviceClientEndpoint.Split(';').Single(s => s.StartsWith("HostName=")).Substring(9);

                return setting;
            }
            catch (Exception)
            {
                Console.WriteLine("Register devices and send message to IotHub in given interval and given format.");
                Console.WriteLine("");
                Console.WriteLine("DeviceLoad.exe {OutputStorageConnectionString} {DeviceClientEndpoint} {DevicePerVm} {MessagePerMin} {DurationInMin} {BatchJobId} {DeviceIdPrefix} [MessageFormat]");
                Console.WriteLine("");
                Console.WriteLine("OutputStorageConnectionString: DefaultEndpointsProtocol=https;AccountName={name};AccountKey={key};EndpointSuffix={core.windows.net}");
                Console.WriteLine("DeviceClientEndpoint: HostName={http://xx.chinacloudapp.cn};SharedAccessKeyName=owner;SharedAccessKey={key}");
                Console.WriteLine("DeviceIdPrefix: It is used to register device: {DeviceIdPrefix}-0000, {DeviceIdPrefix}-{DevicePerVm}.");
                Console.WriteLine("Message: {\"value\": %value%,\"datetime\": \"%datetime%\"}");

                return null;
            }
        }

        public string OutputStorageConnectionString { get; set; }

        public string DeviceClientEndpoint { get; set; }

        public int DevicePerVm { get; set; }

        public int MessagePerMin { get; set; }

        public int DurationInMin { get; set; }

        public string BatchJobId { get; set; }

        public string DeviceIdPrefix { get; set; }

        public string Message { get; set; }

        public RegistryManager IotHubManager { get; private set; }

        public string IoTHubHostName { get; private set; }

        public bool ReadBlobSwitch { get; private set; }
    }
}
