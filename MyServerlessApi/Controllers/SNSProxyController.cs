using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyLambda.Entities;
using Newtonsoft.Json;

namespace MyServerlessApi.Controllers
{
    [Route("api/[controller]")]
    public class SNSProxyController : Controller
    {
        public IAmazonSimpleNotificationService SNSClient { get; }
        public IConfiguration Configuration { get; }
        public ILogger<SNSProxyController> Logger { get; }

        public SNSProxyController(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SNSProxyController> logger)
        {
            SNSClient = snsClient;
            Configuration = configuration;
            Logger = logger;
        }

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
    }
}
