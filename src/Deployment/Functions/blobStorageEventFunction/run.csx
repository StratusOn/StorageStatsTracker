#r "System.Web"
#r "System.Configuration"
#r "System.Data"

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq; 
using Newtonsoft.Json.Serialization; 
using Microsoft.Azure.EventGrid.Models; 

public class SubscriptionValidationEventData 
{ 
    public string ValidationCode { get; set; } 
} 

public class SubscriptionValidationResponseData 
{ 
    public string ValidationResponse { get; set; } 
} 

public class BlobStorageCreatedEventData
{
    public string api { get; set; }
    public string clientRequestId { get; set; }
    public string requestId { get; set; }
    public string eTag { get; set; }
    public string contentType { get; set; }
    public int contentLength { get; set; }
    public string blobType { get; set; }
    public string url { get; set; }
    public string sequencer { get; set; }
    public StorageDiagnostics storageDiagnostics { get; set; }
}

public class BlobStorageDeletedEventData
{
    public string api { get; set; }
    public string requestId { get; set; }
    public string contentType { get; set; }
    public string blobType { get; set; }
    public string url { get; set; }
    public string sequencer { get; set; }
    public StorageDiagnostics storageDiagnostics { get; set; }
}

public class StorageDiagnostics
{
    public string batchId { get; set; }
}

public class ResourceEventData 
{ 
    public string correlationId { get; set; }     
    public object httpRequest { get; set; }   
    public string resourceProvider { get; set; }
    public string resourceUri { get; set; }
    public string operationName { get; set; }
    public string status { get; set; }
    public string subscriptionId { get; set; }
    public string tenantId { get; set; }
} 

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log) 
{ 
    log.Info($"C# HTTP trigger function - Blob created/deleted");

    // Based on: https://docs.microsoft.com/en-us/azure/event-grid/receive-events
    string response = string.Empty; 
    const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";
    const string BlobCreatedEventType = "Microsoft.Storage.BlobCreated";
    const string BlobDeletedEventType = "Microsoft.Storage.BlobDeleted";
    const string StorageAccountDeletedEventType = "Microsoft.Resources.ResourceDeleteSuccess";

    string requestContent = await req.Content.ReadAsStringAsync(); 
    EventGridEvent[] eventGridEvents = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent); 

    foreach (EventGridEvent eventGridEvent in eventGridEvents) 
    { 
        JObject dataObject = eventGridEvent.Data as JObject; 

        // Deserialize the event data into the appropriate type based on event type 
        if (string.Equals(eventGridEvent.EventType, SubscriptionValidationEvent, StringComparison.OrdinalIgnoreCase)) 
        { 
            var eventData = dataObject.ToObject<SubscriptionValidationEventData>(); 
            log.Info($"Got SubscriptionValidation event data, validation code: {eventData.ValidationCode}, topic: {eventGridEvent.Topic}"); 
            // Do any additional validation (as required) and then return back the below response 
            var responseData = new SubscriptionValidationResponseData(); 
            responseData.ValidationResponse = eventData.ValidationCode; 
            return req.CreateResponse(HttpStatusCode.OK, responseData);    
        }
        else
        {
            log.Info($"EventType: {eventGridEvent.EventType}"); 
            log.Info($"Subject: {eventGridEvent.Subject}");
            log.Info($"Topic: {eventGridEvent.Topic}");  
            log.Info($"EventTime: {eventGridEvent.EventTime}");  
            log.Info($"Id: {eventGridEvent.Id}");
            log.Info($"DataVersion: {eventGridEvent.DataVersion}");
            log.Info($"MetadataVersion: {eventGridEvent.MetadataVersion}");

            if (string.Equals(eventGridEvent.EventType, BlobCreatedEventType, StringComparison.OrdinalIgnoreCase))
            {
                var eventData = dataObject.ToObject<BlobStorageCreatedEventData>();

                log.Info($"Data.api: {eventData.api}");
                log.Info($"Data.clientRequestId: {eventData.clientRequestId}");  
                log.Info($"Data.requestId: {eventData.requestId}");  
                log.Info($"Data.eTag: {eventData.eTag}");  
                log.Info($"Data.contentType: {eventData.contentType}");  
                log.Info($"Data.contentLength: {eventData.contentLength}");  
                log.Info($"Data.blobType: {eventData.blobType}");
                log.Info($"Data.url: {eventData.url}");  
                log.Info($"Data.sequencer: {eventData.sequencer}");  
                log.Info($"Data.storageDiagnostics.batchId: {eventData.storageDiagnostics.batchId}"); 

                await WriteBlobCreatedEventToSqlDatabase(eventGridEvent, eventData, log); 
            }
            else if (string.Equals(eventGridEvent.EventType, BlobDeletedEventType, StringComparison.OrdinalIgnoreCase))
            {
                var eventData = dataObject.ToObject<BlobStorageDeletedEventData>();

                log.Info($"Data.api: {eventData.api}");
                log.Info($"Data.requestId: {eventData.requestId}");  
                log.Info($"Data.contentType: {eventData.contentType}");  
                log.Info($"Data.blobType: {eventData.blobType}");  
                log.Info($"Data.url: {eventData.url}");  
                log.Info($"Data.sequencer: {eventData.sequencer}");  
                log.Info($"Data.storageDiagnostics.batchId: {eventData.storageDiagnostics.batchId}");  

                await WriteBlobDeletedEventToSqlDatabase(eventGridEvent, eventData, log); 
            }
            else if (string.Equals(eventGridEvent.EventType, StorageAccountDeletedEventType, StringComparison.OrdinalIgnoreCase))
            {
                var eventData = dataObject.ToObject<ResourceEventData>();
                eventData.httpRequest = eventData.httpRequest ?? "";

                log.Info($"Data.correlationId: {eventData.correlationId}");
                log.Info($"Data.httpRequest: {eventData.httpRequest}");  
                log.Info($"Data.resourceProvider: {eventData.resourceProvider}");  
                log.Info($"Data.resourceUri: {eventData.resourceUri}");  
                log.Info($"Data.operationName: {eventData.operationName}");  
                log.Info($"Data.status: {eventData.status}");  
                log.Info($"Data.subscriptionId: {eventData.subscriptionId}");  
                log.Info($"Data.tenantId: {eventData.tenantId}");

                await WriteStorageAccountDeletedEventToSqlDatabase(eventGridEvent, log); 
            }
        }
    } 

    return req.CreateResponse(HttpStatusCode.OK, response);     
}

public static async Task WriteBlobCreatedEventToSqlDatabase(EventGridEvent eventGridEvent, BlobStorageCreatedEventData eventData, TraceWriter log)
{
    SqlCommand sqlCommand = new SqlCommand("[dbo].[InsertBlobStorageEventDataItem]");
    sqlCommand.CommandType = CommandType.StoredProcedure;
    sqlCommand.Parameters.Add(new SqlParameter("@topic", eventGridEvent.Topic));
    sqlCommand.Parameters.Add(new SqlParameter("@subject", eventGridEvent.Subject));
    sqlCommand.Parameters.Add(new SqlParameter("@eventType", eventGridEvent.EventType));
    sqlCommand.Parameters.Add(new SqlParameter("@eventTime", eventGridEvent.EventTime));
    sqlCommand.Parameters.Add(new SqlParameter("@id", eventGridEvent.Id));
    sqlCommand.Parameters.Add(new SqlParameter("@dataVersion", eventGridEvent.DataVersion));
    sqlCommand.Parameters.Add(new SqlParameter("@metadataVersion", eventGridEvent.MetadataVersion));

    sqlCommand.Parameters.Add(new SqlParameter("@data_api", eventData.api));
    sqlCommand.Parameters.Add(new SqlParameter("@data_clientRequestId", eventData.clientRequestId));
    sqlCommand.Parameters.Add(new SqlParameter("@data_requestId", eventData.requestId));
    sqlCommand.Parameters.Add(new SqlParameter("@data_eTag", eventData.eTag));
    sqlCommand.Parameters.Add(new SqlParameter("@data_contentType", eventData.contentType));
    sqlCommand.Parameters.Add(new SqlParameter("@data_contentLength", eventData.contentLength));
    sqlCommand.Parameters.Add(new SqlParameter("@data_blobType", eventData.blobType));
    sqlCommand.Parameters.Add(new SqlParameter("@data_url", eventData.url));
    sqlCommand.Parameters.Add(new SqlParameter("@data_sequencer", eventData.sequencer));
    sqlCommand.Parameters.Add(new SqlParameter("@data_storageDiagnostics_batchId", eventData.storageDiagnostics.batchId));

    await RunSqlCommand(sqlCommand, log);
}

public static async Task WriteBlobDeletedEventToSqlDatabase(EventGridEvent eventGridEvent, BlobStorageDeletedEventData eventData, TraceWriter log)
{
    SqlCommand sqlCommand = new SqlCommand("[dbo].[InsertBlobStorageEventDataItem]");
    sqlCommand.CommandType = CommandType.StoredProcedure;
    sqlCommand.Parameters.Add(new SqlParameter("@topic", eventGridEvent.Topic));
    sqlCommand.Parameters.Add(new SqlParameter("@subject", eventGridEvent.Subject));
    sqlCommand.Parameters.Add(new SqlParameter("@eventType", eventGridEvent.EventType));
    sqlCommand.Parameters.Add(new SqlParameter("@eventTime", eventGridEvent.EventTime));
    sqlCommand.Parameters.Add(new SqlParameter("@id", eventGridEvent.Id));
    sqlCommand.Parameters.Add(new SqlParameter("@dataVersion", eventGridEvent.DataVersion));
    sqlCommand.Parameters.Add(new SqlParameter("@metadataVersion", eventGridEvent.MetadataVersion));

    sqlCommand.Parameters.Add(new SqlParameter("@data_api", eventData.api));
    sqlCommand.Parameters.Add(new SqlParameter("@data_requestId", eventData.requestId));
    sqlCommand.Parameters.Add(new SqlParameter("@data_contentType", eventData.contentType));
    sqlCommand.Parameters.Add(new SqlParameter("@data_blobType", eventData.blobType));
    sqlCommand.Parameters.Add(new SqlParameter("@data_url", eventData.url));
    sqlCommand.Parameters.Add(new SqlParameter("@data_sequencer", eventData.sequencer));
    sqlCommand.Parameters.Add(new SqlParameter("@data_storageDiagnostics_batchId", eventData.storageDiagnostics.batchId));

    await RunSqlCommand(sqlCommand, log);
}

public static async Task WriteStorageAccountDeletedEventToSqlDatabase(EventGridEvent eventGridEvent, TraceWriter log)
{
    SqlCommand sqlCommand = new SqlCommand("[dbo].[InsertBlobStorageEventDataItem]");
    sqlCommand.CommandType = CommandType.StoredProcedure;
    sqlCommand.Parameters.Add(new SqlParameter("@topic", eventGridEvent.Topic));
    sqlCommand.Parameters.Add(new SqlParameter("@subject", eventGridEvent.Subject));
    sqlCommand.Parameters.Add(new SqlParameter("@eventType", eventGridEvent.EventType));
    sqlCommand.Parameters.Add(new SqlParameter("@eventTime", eventGridEvent.EventTime));
    sqlCommand.Parameters.Add(new SqlParameter("@id", eventGridEvent.Id));
    sqlCommand.Parameters.Add(new SqlParameter("@dataVersion", eventGridEvent.DataVersion));
    sqlCommand.Parameters.Add(new SqlParameter("@metadataVersion", eventGridEvent.MetadataVersion));
    sqlCommand.Parameters.Add(new SqlParameter("@data_api", string.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_requestId", Guid.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_contentType", string.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_blobType", string.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_url", string.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_sequencer", string.Empty));
    sqlCommand.Parameters.Add(new SqlParameter("@data_storageDiagnostics_batchId", Guid.Empty));
 
    var data = JsonConvert.SerializeObject(eventGridEvent.Data); 
    sqlCommand.Parameters.Add(new SqlParameter("@storageAccountDeletedData", data));

    await RunSqlCommand(sqlCommand, log);
}

public static async Task RunSqlCommand(SqlCommand sqlCommand, TraceWriter log)
{
    var connectionString = ConfigurationManager.ConnectionStrings["CONSUMPTION_SQLDB_CONNECTIONSTRING"].ConnectionString;
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        connection.Open();
        sqlCommand.Connection = connection;
        using (SqlCommand command = sqlCommand)
        {
            await command.ExecuteNonQueryAsync();
            log.Info($"Successfully executed stored proc.");
        }
    }
}
