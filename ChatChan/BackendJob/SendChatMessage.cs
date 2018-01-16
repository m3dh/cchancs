namespace ChatChan.BackendJob
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    public class SendChatMessageProcessor : IJobProcessor<SendChatMessageEvent>
    {
        public SendChatMessageProcessor()
        {
        }

        public Task Process(SendChatMessageEvent message)
        {
            throw new NotImplementedException();
        }
    }
}
