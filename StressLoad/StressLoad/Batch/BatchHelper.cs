using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Batch.FileStaging;
using Microsoft.Azure.IoT.Studio.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.Studio.WebJob
{
    public static class BatchHelper
    {
        private static IBatchConnector connector;

        // in test, we can use mock connector
        public static IBatchConnector Connector
        {
            get
            {
                if( connector == null)
                {
                    connector = new BatchConnector();
                }

                return connector;
            }
            set
            {
                connector = value;
            }
        }

        public static string StorageConnectionString { get { return Connector.StorageConnectionString; } }

        public static async Task<bool> Deploy(this TestJob testJob)
        {
            return await Connector.Deploy(testJob);
        }

        public static async Task<TestJobStatus> GetStatus(this TestJob testJob)
        {
            return await Connector.GetStatus(testJob);
        }

        public static async Task<bool> DeleteTest(this TestJob testJob)
        {
            return await Connector.DeleteTest(testJob);
        }

        public static async Task<bool> DeleteTest(this BatchClient client, TestJob testJob)
        {
            return await Connector.DeleteTest(client, testJob);
        }
    }
}
