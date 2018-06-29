using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace BlobStorageEventFunctionApp
{
    public static class BlobStorageEventFunctionImplementation
    {
        public static Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
        {
            return Task.Run(() => new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
