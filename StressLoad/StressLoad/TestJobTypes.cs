using System;
using System.Linq;

namespace Microsoft.Azure.IoT.Studio.Data
{
    public enum TestJobStatus
    {
        Unknown,
        Created,
        Provisioning,
        Enqueued,
        Running,
        Finished,
        Failed,
        VerificationPassed,
        VerificationFailed
    }

    public enum TestJobReceiverType
    {
        BlobStorage,
        TableStorage
    }

    public enum SizeOfVMType
    {
        small,  // 1 core /1.75G
        medium, // 2 core /3.5G
        large,  // 4 core / 7G
        extralarge  // 8 core / 14G
    }

    public static class TestJobStatusExtension
    {
        static private TestJobStatus[] SendCompletedStatus = new TestJobStatus[]
        {
            TestJobStatus.Finished,
            TestJobStatus.Failed,
            TestJobStatus.VerificationPassed,
            TestJobStatus.VerificationFailed
        };

        static private TestJobStatus[] ReceiveCompletedStatus = new TestJobStatus[]
        {
            TestJobStatus.Finished,
            TestJobStatus.Failed,
            TestJobStatus.VerificationPassed,
            TestJobStatus.VerificationFailed
        };

        static public bool IsSendCompleted(this TestJobStatus status)
        {
            return SendCompletedStatus.Contains(status);
        }

        static public bool IsReceiveCompleted(this TestJobStatus status)
        {
            return ReceiveCompletedStatus.Contains(status);
        }
    }

    public class TestJobConfig
    {
        public bool UserCustomerSubsciprtion { get; set; } = false;

        public int NumbeOfVM { get; set; } = 1;

        public int NumbeOfDevicePerVm { get; set; } = 10;

        public int NumbeOfMsgPerDevicePerMin { get; set; } = 10;

        public int DurationInMin { get; set; } = 5;

        public string Message { get; set; } = "";
    }

    public class TestJobResult
    {
        public TestJobStatus Status { get; set; }

        public DateTime Created { get; set; }

        public DateTime SendStarted { get; set; }

        public DateTime SendFinished { get; set; }

        public DateTime FirstReceived { get; set; }

        public DateTime LastReceived { get; set; }

        public long TotalMsgSent { get; set; } = 0;

        public long TotalMsgReceived { get; set; } = 0;
    }

    public class TestJobDeviceResult
    {
        public string DeviceId { get; set; }

        public long TotalMsgSent { get; set; } = 0;

        public long TotalMsgReceived { get; set; } = 0;

        public DateTime SendStarted { get; set; }

        public DateTime SendFinished { get; set; }

        public DateTime FirstReceived { get; set; }

        public DateTime LastReceived { get; set; }
    }
}
