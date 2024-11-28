using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json;
using Amazon.Runtime;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SimpleLambdaFunction;

public class Function
{
    public async Task<LambdaResponse> FunctionHandler(LambdaRequest request, ILambdaContext context)
    {
        var config = new AmazonDynamoDBConfig()
        {
            RegionEndpoint = Amazon.RegionEndpoint.EUCentral1
        };

        var client = new AmazonDynamoDBClient(config);
        var tableName = Environment.GetEnvironmentVariable("target_table");

        var response = new LambdaResponse();
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
