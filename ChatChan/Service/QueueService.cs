namespace ChatChan.Service
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    public interface IQueueService
    {
        Task PushSendMessage(SendMessageQueueEvent sendMessage);
    }

    public class QueueService : IQueueService
    {
        private readonly IMessageQueue<SendMessageQueueEvent> sendMessageQueue;

        public QueueService(IMessageQueue<SendMessageQueueEvent> sendMessageQueue)
        {
            this.sendMessageQueue = sendMessageQueue ?? throw new ArgumentNullException(nameof(sendMessageQueue));
        }

        public Task PushSendMessage(SendMessageQueueEvent sendMessage)
        {
            if (sendMessage == null)
            {
                throw new ArgumentNullException(nameof(sendMessage));
            }

            return this.sendMessageQueue.PushOne(sendMessage);
        }
    }
}
