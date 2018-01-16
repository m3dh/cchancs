namespace ChatChan.Service
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Provider.Queue;

    public interface IQueueService
    {
        Task SendChatMessage();
    }

    public class QueueService : IQueueService
    {
        private readonly IMessageQueue messageQueue;

        public QueueService(IMessageQueue messageQueue)
        {
            this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public Task SendChatMessage()
        {
            return this.messageQueue.PushOne(0, "");
        }
    }
}
