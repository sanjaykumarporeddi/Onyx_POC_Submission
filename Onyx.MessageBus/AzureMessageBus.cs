using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Onyx.MessageBus
{
    public class AzureMessageBus : IMessageBus
    {
        private readonly string _serviceBusConnectionString;
        private readonly ILogger<AzureMessageBus> _logger;
        private const string ServiceBusConnectionStringConfigKey = "AzureServiceBusConnectionString";

        public AzureMessageBus(IConfiguration configuration, ILogger<AzureMessageBus> logger)
        {
            _logger = logger;
            _serviceBusConnectionString = configuration.GetConnectionString(ServiceBusConnectionStringConfigKey)
                                          ?? configuration[ServiceBusConnectionStringConfigKey];

            if (string.IsNullOrEmpty(_serviceBusConnectionString))
            {
                var errorMessage = $"{ServiceBusConnectionStringConfigKey} is not configured. Message publishing will fail.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        public async Task PublishMessage(object message, string topicOrQueueName)
        {
            if (string.IsNullOrEmpty(topicOrQueueName))
                throw new ArgumentNullException(nameof(topicOrQueueName));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _logger.LogInformation("Attempting to publish message of type {MessageType} to {TopicOrQueueName}", message.GetType().Name, topicOrQueueName);

            await using var client = new ServiceBusClient(_serviceBusConnectionString);
            ServiceBusSender sender = client.CreateSender(topicOrQueueName);

            try
            {
                var jsonMessage = JsonConvert.SerializeObject(message);
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonMessage))
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    ContentType = ContentTypes.ApplicationJson
                };

                await sender.SendMessageAsync(serviceBusMessage);
                _logger.LogInformation("Message published successfully to {TopicOrQueueName}. CorrelationId: {CorrelationId}",
                    topicOrQueueName, serviceBusMessage.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to {TopicOrQueueName}. Message type: {MessageType}",
                    topicOrQueueName, message.GetType().Name);
                throw;
            }
            finally
            {
                await sender.DisposeAsync();
            }
        }
    }
    public static class ContentTypes
    {
        public const string PlainTextUtf8 = "text/plain; charset=utf-8";
        public const string JsonPatch = "application/json-patch+json";
        public const string ApplicationJson = "application/json";
        public const string ApplicationProblemJson = "application/problem+json";
    }
}