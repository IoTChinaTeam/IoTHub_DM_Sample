using Microsoft.Azure.Devices;
using Microsoft.Azure.IoT.Studio.Data;
using Microsoft.Azure.IoT.Studio.WebJob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            // if necessary
            if (args.Length > 0 && args[0] == "clean")
            {
                RemoveDevices().Wait();
            }

            var devicePerVm = int.Parse(ConfigurationManager.AppSettings["DevicePerVm"]);
            var numofVM = int.Parse(ConfigurationManager.AppSettings["NumofVm"]);
            TestJob job = new TestJob
            {
                JobId = DateTime.UtcNow.Ticks,
                DevicePerVm = devicePerVm,
                Message = ConfigurationManager.AppSettings["Message"],
                MessagePerMin = int.Parse(ConfigurationManager.AppSettings["MessagePerMin"]),
                NumofVm = numofVM,
                SizeOfVM = (SizeOfVMType)Enum.Parse(typeof(SizeOfVMType), ConfigurationManager.AppSettings["SizeOfVM"]),
                DeviceClientEndpoint = ConfigurationManager.AppSettings["DeviceClientEndpoint"],
                DurationInMin = int.Parse(ConfigurationManager.AppSettings["DurationInMin"])
            };

            job.Deploy().Wait();
            job.DeleteTest().Wait();
            Console.WriteLine(" complete the test");
        }

        static string GetDeviceid(string deviceIdPrefix, int vmIndex, int deviceIndex)
        {
            return $"{deviceIdPrefix}-{vmIndex.ToString().PadLeft(4, '0')}-{deviceIndex.ToString().PadLeft(10, '0')}";
        }

        static async Task RemoveDevices()
        {
            Console.WriteLine($"Start to remove devices.");

            var deviceClientEndpoint = ConfigurationManager.AppSettings["DeviceClientEndpoint"];
            var iotHubManager = RegistryManager.CreateFromConnectionString(deviceClientEndpoint);

            var sw = Stopwatch.StartNew();
            int count = 0;
            while (true)
            {
                var devices1 = new List<Device>(await iotHubManager.GetDevicesAsync(10000));
                if (devices1.Count == 0)
                {
                    break;
                }
                while (devices1.Count > 0)
                {
                    var length = devices1.Count < 100 ? devices1.Count : 100;
                    count += length;
                    await iotHubManager.RemoveDevices2Async(devices1.GetRange(0, length));
                    devices1.RemoveRange(0, length);

                    Console.WriteLine($"{sw.Elapsed.TotalSeconds}:{count} devices removed.");
                }
            }
        }
    }
}
