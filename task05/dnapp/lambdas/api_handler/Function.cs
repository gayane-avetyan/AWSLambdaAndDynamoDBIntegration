using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SimpleLambdaFunction;

public class Function
{
    private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
    private static string tableName = Environment.GetEnvironmentVariable("target_table");
    
    public async Task<LambdaResponse> FunctionHandler(LambdaRequest request, ILambdaContext context)
    {
        LambdaResponse response = new LambdaResponse();
        try
        {
            string eventId = Guid.NewGuid().ToString();
            string createdAt = DateTime.UtcNow.ToString("o");

            var document = new Document
            {
                ["id"] = eventId,
                ["principalId"] = request.PrincipalId,
                ["createdAt"] = createdAt,
                ["body"] = Document.FromJson(JsonConvert.SerializeObject(request.Content))
            };

            Table table = Table.LoadTable(client, tableName);
            await table.PutItemAsync(document);

            Event savedEvent = new Event
            {
                Id = eventId,
                PrincipalId = request.PrincipalId,
                CreatedAt = createdAt,
                Content = request.Content
            };

            response.StatusCode = 201;
            response.Event = savedEvent;
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error: {ex.Message}");
            response.StatusCode = 500;
            response.Event = null;
        }

        return response;
    }
}

public class LambdaRequest
{
    public int PrincipalId { get; set; }
    public Dictionary<string, string> Content { get; set; }
}

public class LambdaResponse
{
    public int StatusCode { get; set; }
    public Event Event { get; set; }
}

public class Event
{
    public string Id { get; set; }
    public int PrincipalId { get; set; }
    public string CreatedAt { get; set; }
    public Dictionary<string, string> Content { get; set; }
}
