using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace BlobStorageEventFunctionApp
{
    public static class BlobStorageEventFunction
    {
        [FunctionName("blobStorageEventFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"blobStorageEventFunction called.");
            return await BlobStorageEventFunctionImplementation.Run(req, log);
        }
    }
}
