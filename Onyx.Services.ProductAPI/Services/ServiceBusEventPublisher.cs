using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onyx.MessageBus;
using Onyx.Services.ProductAPI.Events;
using System.Threading.Tasks;
using Onyx.Services.ProductAPI.Common;

namespace Onyx.Services.ProductAPI.Services
{
    public class ServiceBusEventPublisher : IEventPublisher
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<ServiceBusEventPublisher> _logger;
        private readonly string _productChangesTopicName;
        private const string ProductChangesTopicConfigKey = "ProductChangesTopic";

        public ServiceBusEventPublisher(
            IMessageBus messageBus,
            IConfiguration configuration,
            ILogger<ServiceBusEventPublisher> logger)
        {
            _messageBus = messageBus;
            _logger = logger;
            _productChangesTopicName = configuration[$"{AppConstants.ConfigSections.ServiceBusTopics}:{ProductChangesTopicConfigKey}"];
            if (string.IsNullOrEmpty(_productChangesTopicName))
            {
                _logger.LogWarning("'{ConfigKey}' for product changes topic is not configured. Defaulting to 'product-updates-topic-default'.",
                    $"{AppConstants.ConfigSections.ServiceBusTopics}:{ProductChangesTopicConfigKey}");
                _productChangesTopicName = "product-updates-topic-default";
            }
        }

        public async Task PublishProductChangedEventAsync(ProductChangedEvent productChangedEvent)
        {
            _logger.LogInformation("Attempting to publish ProductChangedEvent for ProductId {ProductId} to topic {TopicName}",
                productChangedEvent.ProductId, _productChangesTopicName);
            try
            {
                await _messageBus.PublishMessage(productChangedEvent, _productChangesTopicName);
                _logger.LogInformation("Successfully published ProductChangedEvent for ProductId {ProductId} to topic {TopicName}",
                    productChangedEvent.ProductId, _productChangesTopicName);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error publishing ProductChangedEvent for ProductId {ProductId} to topic {TopicName}",
                    productChangedEvent.ProductId, _productChangesTopicName);
                throw;
            }
        }
    }
}