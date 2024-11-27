using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using System;
using Newtonsoft.Json;
using System.Net;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SimpleLambdaFunction;

public class Function
{
    private readonly Table _dynamoTable;

    public Function()
    {
        var dynamoClient = new AmazonDynamoDBClient();
        _dynamoTable = Table.LoadTable(dynamoClient, "Events");
    }

    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        // Parse the incoming request
        var requestBody = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.Body);
        var principalId = Convert.ToInt32(requestBody["principalId"]);
        var content = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody["content"].ToString());

        // Create a new event item
        var newEvent = new Document
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["principalId"] = principalId,
            ["createdAt"] = DateTime.UtcNow.ToString("o"),
            ["body"] = JsonConvert.SerializeObject(content)
        };

        // Save the event to DynamoDB
        _dynamoTable.PutItemAsync(newEvent).Wait();

        // Prepare the response object
        var response = new
        {
            id = newEvent["id"],
            principalId = newEvent["principalId"],
            createdAt = newEvent["createdAt"],
            body = content
        };

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 201,
            Body = JsonConvert.SerializeObject(new { statusCode = 201, @event = response }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
