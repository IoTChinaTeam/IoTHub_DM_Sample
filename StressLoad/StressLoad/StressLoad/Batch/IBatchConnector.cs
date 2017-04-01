using Microsoft.Azure.Batch;
using Microsoft.Azure.IoT.Studio.Data;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.Studio.WebJob
{
    public interface IBatchConnector
    {
        string StorageConnectionString { get; }
        
        Task<bool> Deploy(TestJob testJob);

        Task<TestJobStatus> GetStatus(TestJob testJob);

        Task<bool> DeleteTest(TestJob testJob);

        Task<bool> DeleteTest(BatchClient client, TestJob testJob);
    }
}
