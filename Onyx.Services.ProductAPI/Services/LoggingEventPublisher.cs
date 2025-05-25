using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Onyx.Services.ProductAPI.Events;
using Onyx.Services.ProductAPI.Common;

namespace Onyx.Services.ProductAPI.Services
{
    public class LoggingEventPublisher : IEventPublisher
    {
        private readonly ILogger<LoggingEventPublisher> _logger;

        public LoggingEventPublisher(ILogger<LoggingEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishProductChangedEventAsync(ProductChangedEvent productChangedEvent)
        {
            var eventJson = JsonSerializer.Serialize(productChangedEvent);
            _logger.LogInformation(AppConstants.LogMessages.EventPublishing, eventJson);
            return Task.CompletedTask;
        }
    }
}