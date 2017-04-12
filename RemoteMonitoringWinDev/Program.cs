using System;

namespace Microsoft.Azure.Devices.Applications.RemoteMonitoring.WinDev
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = @"HostName=xzshengdmdev.azure-devices.net;DeviceId=xzshengWinDev01;SharedAccessKey=bcc+7k6m8BKUKGaY5Uh/GYQ29iPrm4tcKOsGx4i+J40=";

            var device = new Device(connectionString);
            device.Run();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            device.Stop();
        }
    }
}
