using System;

namespace IoTHubReaderSample
{
    class Settings
    {
        public string ConnectionString { get; private set; }
        public string Path { get; private set; }
        public string PartitionId { get; private set; }
        public string GroupName { get; private set; }
        public DateTime StartingDateTimeUtc { get; private set; }
        public string DeviceID { get; private set; }
        public bool AsyncEventProcess { get; private set; }

        public Settings(CommandArguments parsedArguments)
        {
            ConnectionString = parsedArguments.ConnectionString;
            Path = parsedArguments.Path;
            PartitionId = parsedArguments.PartitionId;
            GroupName = parsedArguments.GroupName;
            StartingDateTimeUtc = DateTime.UtcNow - TimeSpan.FromMinutes(parsedArguments.OffsetInMinutes);
            DeviceID = parsedArguments.DeviceID;
            AsyncEventProcess = parsedArguments.AsyncEventProcess;
        }
    }
}
