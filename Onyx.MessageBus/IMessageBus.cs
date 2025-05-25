using System.Threading.Tasks;

namespace Onyx.MessageBus
{
    public interface IMessageBus
    {
        Task PublishMessage(object message, string topicOrQueueName);
    }
}