#r "System.Web"

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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

public class ResourceEventData 
{ 
    public string correlationId { get; set; }     
    public string httpRequest { get; set; }   
    public string resourceProvider { get; set; }
    public string resourceUri { get; set; }
    public string operationName { get; set; }
    public string status { get; set; }
    public string subscriptionId { get; set; }
    public string tenantId { get; set; }
} 

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log) 
{ 
    log.Info($"C# HTTP trigger function - Storage Account created/deleted");

    // Based on: https://docs.microsoft.com/en-us/azure/event-grid/receive-events
    string response = string.Empty; 
    const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";
    const string StorageResourceProvider = "Microsoft.Storage";
    const string ResourceCreatedEvent = "Microsoft.Resources.ResourceWriteSuccess";
    const string ResourceDeletedEvent = "Microsoft.Resources.ResourceDeleteSuccess";

    string requestContent = await req.Content.ReadAsStringAsync(); 
    EventGridEvent[] eventGridEvents = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);

    var responseContent = new StringBuilder();
    var responseContentSeparator = "{{\r\n";
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

            var eventData = dataObject.ToObject<ResourceEventData>();

            // Only process events where resourceProvider=Microsoft.Storage.
            if (string.Equals(eventData.resourceProvider, StorageResourceProvider, StringComparison.OrdinalIgnoreCase))
            {
                log.Info($"Data.correlationId: {eventData.correlationId}");
                log.Info($"Data.httpRequest: {eventData.httpRequest}");  
                log.Info($"Data.resourceProvider: {eventData.resourceProvider}");  
                log.Info($"Data.resourceUri: {eventData.resourceUri}");  
                log.Info($"Data.operationName: {eventData.operationName}");  
                log.Info($"Data.status: {eventData.status}");  
                log.Info($"Data.subscriptionId: {eventData.subscriptionId}");  
                log.Info($"Data.tenantId: {eventData.tenantId}");  

                string webHookUri = ConfigurationManager.AppSettings["BLOB_STORAGE_EVENTS_FUNCTIONAPPNAME_ENDPOINT"];
                if (string.IsNullOrWhiteSpace(webHookUri))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("The BLOB_STORAGE_EVENTS_FUNCTIONAPPNAME_ENDPOINT application setting is missing.", Encoding.UTF8, "application/json")
                    };
                }
                log.Info($"webHookUri: {webHookUri}");

                // Create a subscription if a storage account is created.
                if (string.Equals(eventGridEvent.EventType, ResourceCreatedEvent, StringComparison.OrdinalIgnoreCase))
                {
                    var responseMessage = await CreateStorageAccountSubscription(eventData, webHookUri, log);
                    responseContent.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", responseContentSeparator, responseMessage.Content);
                }
                else if (string.Equals(eventGridEvent.EventType, ResourceDeletedEvent, StringComparison.OrdinalIgnoreCase))
                {
                    var responseMessage = await TriggerStorageAccountDeletedEvent(requestContent, webHookUri, log);
                    responseContent.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", responseContentSeparator, responseMessage.Content);
                }
            }

            responseContentSeparator = "\r\n,\r\n";
        }
    } 

    if (responseContent.Length > 0)
    {
        responseContent.Append("\r\n}");
        //log.Info($"Full Response:");
        //log.Info(responseContent.ToString);

        bool returnFullResponse = false;
        bool succeeded = bool.TryParse(ConfigurationManager.AppSettings["RETURN_FULL_RESPONSE"], out returnFullResponse);
        if (succeeded && returnFullResponse)
        {
            response = responseContent.ToString();
        }
    }

    return req.CreateResponse(HttpStatusCode.OK, response);     
}

public static async Task<HttpResponseMessage> CreateStorageAccountSubscription(ResourceEventData eventData, string webHookUri, TraceWriter log) 
{
    const string requestBody = "{\r\n  \"properties\": {\r\n    \"destination\": {\r\n      \"endpointType\": \"WebHook\",\r\n      \"properties\": {\r\n        \"endpointUrl\": \"~~webHookUri~~\"\r\n      }\r\n    },\r\n    \"filter\": {\r\n      \"isSubjectCaseSensitive\": false,\r\n      \"subjectBeginsWith\": \"\",\r\n      \"subjectEndsWith\": \"\"\r\n    }\r\n  }\r\n}";
    string scope = eventData.resourceUri;

    // Parse the storage account name:
    Regex regex = new Regex(@"/subscriptions/(?<SubscriptionId>.+)/resourcegroups/(?<ResourceGroup>.+)/providers/Microsoft.Storage/storageAccounts/(?<StorageAccount>[a-z0-9]{1,24})", 
		RegexOptions.Singleline);
    Match match = regex.Match(eventData.resourceUri);

    var storageAccountName = match.Groups["StorageAccount"].Value;
    var eventSubscriptionName = $"{storageAccountName}-eventsubscription";
    
    // Get a fresh token.
    string msiGetTokenEndpoint = ConfigurationManager.AppSettings["MSI_GETTOKEN_ENDPOINT"];
    if (string.IsNullOrWhiteSpace(msiGetTokenEndpoint))
    {
        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("The MSI_GETTOKEN_ENDPOINT application setting is missing.", Encoding.UTF8, "application/json")
        };
    }
    log.Info($"MSI GetToken Endpoint: {msiGetTokenEndpoint}");
    string accessToken = await InvokeRestMethodAsync(msiGetTokenEndpoint, log, HttpMethod.Get);
    //log.Info($"MSI Token: {accessToken}");

    string createEventSubscription = $"https://management.azure.com{scope}/providers/Microsoft.EventGrid/eventSubscriptions/{eventSubscriptionName}?api-version=2018-01-01";
    
    log.Info($"createEventSubscription: {createEventSubscription}");
    string content = requestBody.Replace("~~webHookUri~~", webHookUri);
    //log.Info($"content: {content}");

    var eventSubscriptionResponse = await InvokeRestMethodAsync(createEventSubscription, log, HttpMethod.Put, content, accessToken);
    log.Info($"eventSubscriptionResponse: {eventSubscriptionResponse}");

    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(eventSubscriptionResponse, Encoding.UTF8, "application/json")
    };
}

public static async Task<HttpResponseMessage> TriggerStorageAccountDeletedEvent(string requestContent, string webHookUri, TraceWriter log) 
{
    log.Info($"Triggering StorageAccountDeleted event.");

    var triggerEventResponse = await InvokeRestMethodAsync(webHookUri, log, HttpMethod.Post, requestContent);
    log.Info($"triggerEventResponse: {triggerEventResponse}");

    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(triggerEventResponse, Encoding.UTF8, "application/json")
    };
}

public static async Task<string> InvokeRestMethodAsync(string url, TraceWriter log, HttpMethod httpMethod, string body = null, string authorizationToken = null, string authorizationScheme = "Bearer", IDictionary<string, string> headers = null, string additionalContentTypeHeaders = "")
{
    HttpClient client = new HttpClient();
    if (!string.IsNullOrWhiteSpace(authorizationToken))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authorizationScheme, authorizationToken);
        //log.Info($"Authorization: {client.DefaultRequestHeaders.Authorization.Parameter}");
    }

    HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
    if (headers != null && headers.Count > 0)
    {
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }
    }

    if (!string.IsNullOrWhiteSpace(body))
    {
        request.Content = new StringContent(body, Encoding.UTF8);
        if (!string.IsNullOrWhiteSpace(additionalContentTypeHeaders))
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/json{additionalContentTypeHeaders}");
        }
        else
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }
    }

    HttpResponseMessage response = await client.SendAsync(request);
    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadAsStringAsync();
    }

    string statusCodeName = response.StatusCode.ToString();
    int statusCodeValue = (int)response.StatusCode;
    string content = await response.Content.ReadAsStringAsync();
    log.Info($"Status Code: {statusCodeName} ({statusCodeValue}). Body: {content}");

    throw new Exception($"Status Code: {statusCodeName} ({statusCodeValue}). Body: {content}");
}
