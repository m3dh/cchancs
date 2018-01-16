namespace ChatChan.BackendJob
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    public class SendMessageProcessor : IJobProcessor
    {
        private readonly IMessageQueue<SendMessageQueueEvent> messageQueue;

        public SendMessageProcessor(IMessageQueue<SendMessageQueueEvent> messageQueue)
        {
            this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public Task<bool> ProcessOne()
        {
            throw new System.NotImplementedException();
        }
    }
}
