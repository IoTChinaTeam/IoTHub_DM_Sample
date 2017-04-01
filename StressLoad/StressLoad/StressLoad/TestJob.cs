using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
//using Microsoft.Azure.IoT.Studio.DataAccess;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;

namespace Microsoft.Azure.IoT.Studio.Data
{
    public class TestJob
    {
        public const string BlobStorageName = "BlobStorageConnectionString";
        public const string TableStorageName = "TableStorageConnectionString";
        public const string IotHubEndpointName = "IoTHubConnectionString";

        // use the same pool for now
        public const string BatchPoolId = "TestPool";

        public bool UseCustomerSubsciprtion { get; set; } = false;

        // to use as table name/job name in batch service
        public string BatchJobId
        {
            get
            {
                return GetBatchJobId(this.JobId);
            }
        }

        public long JobId { get; set; }

        public string SolutionId { get; set; }

        public TestJobStatus? Status { get; set; }

        public int NumofVm { get; set; }

        public SizeOfVMType SizeOfVM { get; set; }

        public int DevicePerVm { get; set; }

        public int MessagePerMin { get; set; }

        public int DurationInMin { get; set; }

        public string Message { get; set; }

        public Dictionary<string, string> SolutionOutputs { get; set; }

        public string ResultStorageConnectionString
        {
            get
            {
                if (SolutionOutputs.ContainsKey(TableStorageName))
                {
                    return SolutionOutputs[TableStorageName];
                }

                if (SolutionOutputs.ContainsKey(BlobStorageName))
                {
                    return SolutionOutputs[BlobStorageName];
                }

                return null;
            }
        }

        public string ResultTableName
        {
            get
            {
                if (SolutionOutputs.ContainsKey(TableStorageName))
                {
                    var accountName = SolutionOutputs[TableStorageName].Split(';').Where(f => f.StartsWith("AccountName=", StringComparison.InvariantCultureIgnoreCase)).First().Split('=')[1];
                    return accountName.Substring(0, accountName.Length - 2) + "table";
                }

                return null;
            }
        }

        public string ResultBlobContainer
        {
            get
            {
                if (SolutionOutputs.ContainsKey(BlobStorageName))
                {
                    var accountName = SolutionOutputs[BlobStorageName].Split(';').Where(f => f.StartsWith("AccountName=", StringComparison.InvariantCultureIgnoreCase)).First().Split('=')[1];
                    return accountName.Substring(0, accountName.Length - 2) + "datacontainer";
                }

                return null;
            }
        }

        public string DeviceClientEndpoint
        {
            get;set;
        }

        public DateTime CreatedDateTime { get; set; }

        public DateTime? RunningStartDateTime { get; set; }

        public DateTime? RunningFinishDateTime { get; set; }

        public DateTime? VerificationStartDateTime { get; set; }

        public DateTime? VerificationFinishDateTime { get; set; }

        public string LockName { get; private set; }

        public DateTime? LockUtil { get; private set; }


        public static string GetBatchJobId(long jobId)
        {
            return string.Format(ConfigurationManager.AppSettings["DeviceIdPrefix"] + jobId.ToString());
        }

        List<string> blobDeviceIds = new List<string>();
        public List<string> GetDeviceIds()
        {
            if (blobDeviceIds.Count > 0 || GetBlobDevices(blobDeviceIds))
                return blobDeviceIds;
            var deviceIds = new List<string>();
            for (int i = 0; i < this.NumofVm; i++)
            {
                for (int j = 0; j < this.DevicePerVm; j++)
                {
                    deviceIds.Add(string.Format("{0}-{1}-{2}", this.BatchJobId, i.ToString().PadLeft(2, '0'), j.ToString().PadLeft(4, '0')));
                }
            }
            return deviceIds;
        }

        private bool GetBlobDevices(List<string> deviceIds)
        {
            try
            {
                CloudBlobContainer container;
                var clearMessage = Message.Replace(";TestJobType=Blob", "");
                if (clearMessage.Contains("ContainerName"))
                {
                    var connectionStringItems = clearMessage.Split(';');
                    var containerName = connectionStringItems.Last().Split('=').Last();
                    var itemList = connectionStringItems.ToList();
                    itemList.RemoveAt(itemList.Count - 1);
                    var clearConnectionString = itemList.Aggregate((a, b) => a + ";" + b);
                    var storageAccount = CloudStorageAccount.Parse(clearConnectionString);
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    container = blobClient.GetContainerReference(containerName);
                }
                else
                {
                    container = new CloudBlobContainer(new Uri(clearMessage));
                }
                foreach (IListBlobItem item in container.ListBlobs(null, false).Where(c => c.GetType() == typeof(CloudBlockBlob)))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    using (StreamReader reader = new StreamReader(blob.OpenRead()))
                    {
                        reader.BaseStream.Position = 0;
                        JToken token = JToken.Parse(reader.ReadLine());
                        deviceIds.Add(token["deviceId"].ToString());
                    }
                }
                return true;
            }
            catch (Exception)
            {
                //TODO: need to trace error
                return false;
            }
        }
    }
}
