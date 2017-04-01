using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace DeviceLoad
{
    public class ResultUpdater
    {
        private readonly string batchJobId;
        private CloudTable table;

        public ResultUpdater(string storageConnectionString, string batchJobId)
        {
            this.batchJobId = batchJobId;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableName = batchJobId.ToLowerInvariant().Replace('-', 'i');

            this.table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

            this.SentCount = new ConcurrentDictionary<string, Tuple<DateTime, DateTime, long>>();

            this.updateTask = this.UpdateTask();
        }

        public ConcurrentDictionary<string, Tuple<DateTime, DateTime, long>> SentCount { get; }

        public void ReportMessages(string deviceId, long totalMessages)
        {
            this.SentCount.AddOrUpdate(deviceId, 
                Tuple.Create(DateTime.UtcNow, DateTime.UtcNow,totalMessages),
                (key, oldValue) => Tuple.Create(oldValue.Item1, DateTime.UtcNow, totalMessages));
        }

        private bool finishFlag = false;
        private Task updateTask;

        public Task Finish()
        {
            finishFlag = true;
            return this.updateTask;
        }

        public async Task<List<DeviceEntity>> ReadAsync()
        {
            var querySendCount = new TableQuery<DeviceEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, this.batchJobId));

            TableContinuationToken token = null;
            var results = new List<DeviceEntity>();

            while (true)
            {
                var queryResults = await table.ExecuteQuerySegmentedAsync(querySendCount, token);
                results.AddRange(queryResults.Results);

                token = queryResults.ContinuationToken;
                if (queryResults.ContinuationToken == null)
                {
                    break;
                }
            }

            return results;
        }

        private async Task UpdateTask()
        {
            var cache = MemoryCache.Default;

            bool lastRound = false;
            while (true)
            {
                if (this.SentCount.Count > 0)
                {
                    TableBatchOperation batchOperation = new TableBatchOperation();

                    var deviceIds = this.SentCount.Keys;
                    foreach (var deviceId in deviceIds)
                    {
                        Tuple<DateTime, DateTime, long> data;
                        if (this.SentCount.TryGetValue(deviceId, out data))
                        {
                            var content = cache[deviceId] as string;
                            if (content == null || string.Compare(content, data.Item3.ToString(), true) != 0)
                            {
                                batchOperation.InsertOrReplace(new DeviceEntity(this.batchJobId, deviceId, data.Item1, data.Item2, data.Item3));
                                cache.Set(deviceId, data.Item3.ToString(), DateTimeOffset.Now.AddHours(1));
                            }
                        }
                    }

                    try
                    {
                        table.ExecuteBatch(batchOperation);
                    }
                    catch (Exception)
                    {
                        foreach (var deviceId in deviceIds)
                        {
                            cache.Remove(deviceId);
                        }
                    }
                }

                if (lastRound)
                {
                    break;
                }

                if (finishFlag)
                {
                    // make sure one more round before break
                    Console.WriteLine("lastRound!");
                    lastRound = true;
                    continue;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }

    public class DeviceEntity : TableEntity
    {
        public DeviceEntity(string jobId, string deviceId, DateTime first, DateTime last, long totalMessages)
            : base(jobId, deviceId)
        {
            this.TotalMessages = totalMessages;
            this.FirstReportDatetime = first;
            this.LastReportDatetime = last;
        }

        public DeviceEntity() { }

        public long TotalMessages { get; set; }

        public DateTime FirstReportDatetime { get; set; }

        public DateTime LastReportDatetime { get; set; }
    }
}
