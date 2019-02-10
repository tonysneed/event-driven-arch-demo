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

## Publish SNS Topic with Web API

> Note: This demo publishes a Serverless app to an API Gateway, but a Web API can also be deployed to Amazon ECS that is accessible by some other means.

1. Create a new AWS Serverless app using the ASPNET Core Web API blueprint.
2. Update the AWS NuGet packages.
   - Update the `AWSSDK.Extensions.NETCore.Setup` package to the current version
   - Remove the `AWSSDK.S3` package.
   - Add the `AWSSDK.SimpleNotificationService` package.
3. Remove `S3` related code from the project.
   - Delete the **S3ProxyController.cs** file
   - Update the call to `services.AddAWSService` in the `Startup` class

    ```cs
    services.AddAWSService<IAmazonSimpleNotificationService>();
    ```

   - Remove S3 related items from the **serverless.template** file.
  
4. Add CloudWatch logging provider
   - Add `AWS.Logger.AspNetCore` NuGet package.
   - Add following to **appsettings.json** file.

    ```json
    "AWS.Logging": {
        "Region": "eu-west-1",
        "LogGroup": "Event-Driven-Arch.MyServerlessApi",
        "LogLevel": {
            "Default": "Debug",
            "Microsoft": "Information"
        }
    },
    ```
   - Add `ILoggerFactory loggerFactory` to the `Configure` method in **Startup.cs**, then call `AddAWSProvider`.

    ```cs
    loggerFactory.AddAWSProvider(Configuration.GetAWSLoggingConfigSection());
    ```

5. Add the **SNSProxyController.cs** file to the **Controllers** folder.
   - Choose the _API Controller - Empty_ template.
6. Add the ARN of the SNS topic to the appsettings.json file.
   - Copy the topic ARN recorded earlier and paste it into **appsettings.json**.

    ```json
    "TopicArn": "arn:aws:sns:eu-west-1:779191825743:demo-topic"
    ```

7. Add a contructor to `SNSProxyController`.
   - Inject required depenedencies.

    ```cs
    public SNSProxyController(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SNSProxyController> logger)
    {
        SNSClient = snsClient;
        Configuration = configuration;
        Logger = logger;
    }
    ```

8. Add an async post method to publish a message to the topic.

    ```cs
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

9.  Publish the serverless app to Amazon.
    - Select the appropriate stack name and S3 bucket for the CloudFormation templates.
    - After publishing has completed copy the AWS Serverless URL from the published service.
10. Enable CloudWatch logs for the API Gateway service.
    - Create a new role and assign `AmazonAPIGatewayPushToCloudWatchLogs` policy.
    - Copy the role ARN and paste into **CloudWatch log role ARN** of Amazon API Gateway Settings.
    - Select the Prod **stage** of the API for the service and enable CloudWatch settings under **Logs/Tracing**.
    - After executing the request in Postman, go to CloudWatch logs and filter for `API-Gateway`
    - If an error response is returned examine the Endpoint response body in the logs.
11. Test the servicve with Postman: POST with raw JSON body

    ```json
    {
        "duration": 5,
        "message": "Hello Lambda! (5 seconds)"
    }
    ```

12. Inspect the CloudWatch logs for the lambda function that processes messages from the queue.
   - The logs should show the message that has been delivered the the queue from the subscription to the SNS topic.
