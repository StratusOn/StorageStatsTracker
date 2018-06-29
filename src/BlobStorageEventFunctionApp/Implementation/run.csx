#load "BlobStorageEventFunctionImplementation.cs"

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

public static Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"blobStorageEventFunction called.");
    return await BlobStorageEventFunctionImplementation.Run(req, log);
}
