namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Provider;
    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    public interface IMessageService
    {
        Task<Message> CreateMessage(ChannelId channelId, AccountId senderId, MessageType type, string uuid, string body);
        Task<IList<Message>> ListMessages(ChannelId channelId, long lastMsgOrdinal, int selection = 150);
        Task<Message> GetMessage(ChannelId channelId, string uuid);
    }

    public class MessageService : IMessageService
    {
        private readonly ILogger logger;
        private readonly IDataPartitionProvider partitionProvider;
        private readonly IChannelService channelService;

        public MessageService(
            ILoggerFactory loggerFactory,
            IChannelService channelService,
            IDataPartitionProvider partitionProvider)
        {
            this.logger = loggerFactory?.CreateLogger<ParticipantService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            this.partitionProvider = partitionProvider ?? throw new ArgumentNullException(nameof(partitionProvider));
        }

        public async Task<Message> GetMessage(ChannelId channelId, string uuid)
        {
            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            int partition = await this.channelService.GetChannelParititon(channelId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            Message message = (await executor.QueryAll<Message>(
                MessageQueries.GetMessage,
                new Dictionary<string, object>
                {
                    { "@channelId", channelId.ToString() },
                    { "@uuid", uuid },
                })).FirstOrDefault();

            if (message == null)
            {
                throw new NotFound($"Message {uuid} is not found in {channelId}");
            }

            return message;
        }

        public async Task<Message> CreateMessage(ChannelId channelId, AccountId senderId, MessageType type, string uuid, string body)
        {
            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (senderId == null)
            {
                throw new ArgumentNullException(nameof(senderId));
            }

            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentException(nameof(uuid));
            }

            if (string.IsNullOrEmpty(body))
            {
                throw new ArgumentException(nameof(body));
            }

            int partition = await this.channelService.GetChannelParititon(channelId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            int affected = 0;
            long msgId = -1;
            try
            {
                (affected, msgId) = await executor.Execute(
                    MessageQueries.CreateMessage,
                    new Dictionary<string, object>
                    {
                        { "@uuid", uuid },
                        { "@type", (int)type },
                        { "@body", body },
                        { "@channelId", channelId.ToString() },
                        { "@senderId", senderId.ToString() },
                    });
            }
            catch (Conflict ex)
            {
                if (ex.ErrorCode != Conflict.Code.Duplication)
                {
                    throw;
                }
            }

            if (affected == 1)
            {
                this.logger.LogDebug("New message (UUID:{0}) created in partition {1} with ID: {2}", uuid, partition, msgId);
            }

            return await this.GetMessage(channelId, uuid);
        }

        public async Task<IList<Message>> ListMessages(ChannelId channelId, long lastMsgOrdinal, int selection = 150)
        {
            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            // Sequence number starts from 1
            if (lastMsgOrdinal < 0)
            {
                throw new BadRequest($"Unexpected message ordinal number {lastMsgOrdinal}");
            }

            int partition = await this.channelService.GetChannelParititon(channelId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            return await executor.QueryAll<Message>(
                MessageQueries.QueryMessageByChannelIdAndDt,
                new Dictionary<string, object>
                {
                    { "@channelId", channelId.ToString() },
                    { "@ordinalNumber", lastMsgOrdinal },
                    { "@selection", selection }
                });
        }
    }
}
