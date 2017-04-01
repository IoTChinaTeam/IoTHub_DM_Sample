using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeviceLoad
{
    public class AzureBlobDataReader : IBlobDataReader
    {
        DateTime _earliesttUtc;
        CloudBlobContainer _container;
        List<BlockBlobInfo> _blobInfoList;
        private const int extends = 10;
        public AzureBlobDataReader(string containerSas, int expiredMinutes = 30)
        {
            if (containerSas.Contains("ContainerName"))
            {
                var connectionStringItems = containerSas.Split(';');
                var containerName = connectionStringItems.Last().Split('=').Last();
                var itemList = connectionStringItems.ToList();
                itemList.RemoveAt(itemList.Count - 1);
                var clearConnectionString = itemList.Aggregate((a, b) => a + ";" + b);
                containerSas = GetBlobSharedAccessSignature(clearConnectionString, containerName, expiredMinutes);
            }
            _container = new CloudBlobContainer(new Uri(containerSas));
            _blobInfoList = new List<BlockBlobInfo>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="earliest"></param>
        /// <returns></returns>
        public IEnumerable<string> GetDevices()
        {
            List<string> result = new List<string>();
            _earliesttUtc = DateTime.Now;
            foreach (IListBlobItem item in _container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    StreamReader reader = new StreamReader(blob.OpenRead());
                    reader.BaseStream.Position = 0;
                    JToken token = JToken.Parse(reader.ReadLine());
                    string deviceId = token["deviceId"].ToString();
                    if (!string.IsNullOrEmpty(deviceId)
                        && !result.Contains(deviceId))
                    {
                        result.Add(deviceId);
                        Console.WriteLine("add deviceId----" + deviceId);
                        _blobInfoList.Add(new BlockBlobInfo()
                        {
                            DeviceId = deviceId,
                            BlockBlob = blob,
                            BlockStream = reader.BaseStream
                        });
                    }
                    else
                    {
                        reader.Close();
                    }
                    if (!string.IsNullOrEmpty(token["utc"].ToString()))
                    {
                        try
                        {
                            DateTime curDeviceUtc = DateTime.FromFileTimeUtc(long.Parse(token["utc"].ToString()));
                            if (curDeviceUtc < _earliesttUtc)
                                _earliesttUtc = curDeviceUtc;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return result;
        }

        public double GetSecondOffset(DateTime baseTime)
        {
            return (baseTime - _earliesttUtc).TotalSeconds;
        }

        public void CloseStreamReaders()
        {
            if (_blobInfoList.Count == 0)
                return;
            _blobInfoList.ForEach(item =>
            {
                try
                {
                    item.BlockStream.Close();
                }
                catch
                {

                }
            });
        }

        public IEnumerable<string> ReadBlobLines(string deviceId, ref long startOffset, uint count)
        {
            List<string> result = new List<string>();
            Console.WriteLine("begin read:" + deviceId + "---" + DateTime.Now.ToString());
            BlockBlobInfo info = _blobInfoList.Single(item => item.DeviceId == deviceId);
            if (info == null)
                return null;
            StreamReader reader = new StreamReader(info.BlockStream);
            info.BlockStream.Position = startOffset;
            DateTime start = DateTime.Now;
            long totalBytes = 0;
            while (count > 0 && !reader.EndOfStream)
            {
                string dataLine = reader.ReadLine();

                if (!string.IsNullOrEmpty(dataLine))
                {
                    startOffset += dataLine.Length + 2;
                    totalBytes += dataLine.Length;
                    result.Add(dataLine);
                    count--;
                }
                else
                    startOffset += 2;
            }
            if (reader.EndOfStream)
                startOffset = 0;
            Console.WriteLine("total time(s):" + deviceId + "---" + (DateTime.Now - start).TotalSeconds);
            Console.WriteLine(deviceId + "---total bytes:" + totalBytes + "---" + totalBytes / 1024.0 + "---" + (totalBytes / 1024.0) / 1024);
            return result;
        }

        private string GetBlobSharedAccessSignature(string storageConnectionString, string containerName, int expiredMinutes)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            var queuePolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTime.UtcNow,
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(expiredMinutes + extends),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List,
            };
            return container.Uri + container.GetSharedAccessSignature(queuePolicy, null);
        }

    }

    public class BlockBlobInfo
    {
        public string DeviceId { get; set; }
        public CloudBlockBlob BlockBlob { get; set; }
        public Stream BlockStream { get; set; }

    }
}
