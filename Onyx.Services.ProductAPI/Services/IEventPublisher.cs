using System.Threading.Tasks;
using Onyx.Services.ProductAPI.Events;

namespace Onyx.Services.ProductAPI.Services
{
    public interface IEventPublisher
    {
        Task PublishProductChangedEventAsync(ProductChangedEvent productChangedEvent);
    }
}