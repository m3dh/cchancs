namespace ChatChan.Service
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;
    using Newtonsoft.Json;

    public interface IQueueService
    {
        Task SendChatMessage(string messageUuid, ChannelId channelId);
    }

    public class QueueService : IQueueService
    {
        private readonly MessageQueueProvider messageQueue;

        public QueueService(MessageQueueProvider messageQueue)
        {
            this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public Task SendChatMessage(string messageUuid, ChannelId channelId)
        {
            if (string.IsNullOrEmpty(messageUuid))
            {
                throw new ArgumentNullException(nameof(messageUuid));
            }

            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            SendChatMessageEvent evt = new SendChatMessageEvent
            {
                Uuid = messageUuid,
                ChannelId = channelId,
            };

            return this.PushOne(ChatAppQueueEventTypes.SendMessage, evt);
        }

        private Task PushOne(ChatAppQueueEventTypes evType, object data)
        {
            return this.messageQueue.PushOne((int)evType, JsonConvert.SerializeObject(data));
        }
    }
}
