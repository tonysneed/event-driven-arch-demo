# Event-Driven Microservices Architecture Demo

Demonstrates event-driven microservices architecture using Amazon SNS and SQS.

## Prerequisites
- .NET Core SDK
- Visual Studio 2017 (or higher)
- AWS Toolkit for Visual Studio

## Overview

In this demo a Serverless Web API publishes an SNS topic, and an SQS queue subscribes to the topic based on a message filter. Messages delivered to the queue are then processed by a Lambda function.

## Lambda Function

1. In Visual Studio create a new AWS Lambda Project (.NET Core)
   - For blueprint select "Simple SQS Function"
2. Flesh out the `ProcessMessageAsync` method.
   - In the example the message body is deserialized.
3. Run the Mock Lambda Test Tool and select SQS for the request type. 
   - Sample body: `"{\"duration\": 3,\"message\": \"Hello Lambda! (3 seconds)\"}"`
4. Deploy the Lambda function.
   - Set the function-timeout (default is 30 seconds).
   - Select new execution role based on AWSLambdaSQSQueueExecutionRole policy

## Source Queue

1. Create an SQS queue
   - You can use the AWS Explorer in Visual Studio if you wish.
   - Set default Visibility Timeout to 6 times the function timeout
     - For example: 30 x 6 = 180
2. Open Event Sources tab of the deployed Lambda function.
   - Click the Add button and select the queue you just created.
3. Send a test message to the queue
   - Go to the cloud watch logs for the lambda function
     - For example: `aws/lambda/demo-sqs-lambda`
   - Right-click on the newly created queue in the AWS Explorer and select Send Message.
     - Enter a message body in JSON format: `{"duration": 4,"message": "Hello Lambda! (4 seconds)"}`

## SNS Topic with SQS Subscription

1. Create an SNS Topic (for example, using the AWS Explorer in Visual Studio)
   - Create a key for the `TopicArn` setting.
   - Make note of the topic ARN.
2. Create a subscription to the topic for the SNS queue.
   - Select SQS as the protocol, select the sample queue.

## Serverless API to publish SNS Topic

1. Create a new AWS Serverless app using the ASPNET Core Web API blueprint.
2. Update the AWS NuGet packages.
   - Update the `AWSSDK.Extensions.NETCore.Setup` package to the current version
   - Remove the `AWSSDK.S3` package.
   - Add the `AWSSDK.SimpleNotificationService` package.
3. Remove `S3` related code from the project.
   - Delete the **S3ProxyController.cs** file
   - Update the call to `services.AddAWSService` in the `Startup` class
    ```csharp
    services.AddAWSService<IAmazonSimpleNotificationService>();
    ```
4. Add the **SNSProxyController.cs** file to the **Controllers** folder.
   - Choose the _API Controller - Empty_ template.
5. Add the ARN of the SNS topic to the appsettings.json file.
   - Copy the topic ARN recorded earlier and paste it into **appsettings.json**.
    ```json
    "TopicArn": "arn:aws:sns:eu-west-1:779191825743:demo-topic"
    ```
6. Add a contructor to `SNSProxyController`.
   - Inject required depenedencies.
    ```csharp
    public SNSProxyController(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SNSProxyController> logger)
    {
        SNSClient = snsClient;
        Configuration = configuration;
        Logger = logger;
    }
    ```
7. Add an async post method to publish a message to the topic.
    ```csharp
    // POST api/snsproxy
    [HttpPost]
    public async Task<IActionResult> Post([FromBody]LambdaMessage message)
    {
        var json = JsonConvert.SerializeObject(message);
        var topicArn = Configuration[Constants.Keys.TopicArn];
        var result = await SNSClient.PublishAsync(topicArn, json);
        Logger.LogInformation($"Published SNS Topic. Message Id: {result.MessageId}");
        return StatusCode((int) result.HttpStatusCode, result.MessageId);
    }
    ```
8. Test the SNS proxy locally.
   - Postman: POST with json body
     - URL: http://localhost:51123/api/snsproxy
    ```json
    {
        "duration": 5,
        "message": "Hello Lambda! (5 seconds)"
    }
    ```
9. Publish the serverless app to Amazon
   - Copy the AWS Serverless URL from the published service.

