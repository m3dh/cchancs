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
    using Newtonsoft.Json;

    public interface IParticipantService
    {
        // Participants are not guaranteed to sync with channel's members, so this is a delayed result.
        Task<IList<Participant>> ListAccountParticipants(AccountId accountId);
        Task<int> LinkAccountWithChannel(AccountId accountId, ChannelId channelId);
        Task UnlinkAccountWithChannel(AccountId accountId, ChannelId channelId);

        // List participants with message info: for the first 'updates' screen usage.
        Task<IList<Participant>> ListAccountParticipantsWithMessageInfo(AccountId accountId, long dtSince);

        // Update participant items with a new message, could be a creation.
        Task<bool> UpdateParticipantWithNewMessage(AccountId accountId, ChannelId channelId, Message message);

        // This is a ack from the message reader - for cross device syncs.
        Task<bool> UpdateParticipantLastReadMessageCount(AccountId accountId, ChannelId channelId, int messageCount);
    }

    public class ParticipantService : IParticipantService
    {
        private readonly ILogger logger;
        private readonly IAccountService accountService;
        private readonly IDataPartitionProvider partitionProvider;

        public ParticipantService(
            ILoggerFactory loggerFactory,
            IAccountService accountService,
            IDataPartitionProvider partitionProvider)
        {
            this.logger = loggerFactory?.CreateLogger<ParticipantService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            this.partitionProvider = partitionProvider ?? throw new ArgumentNullException(nameof(partitionProvider));
        }

        public async Task<int> LinkAccountWithChannel(AccountId accountId, ChannelId channelId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            // Participant records are partitioned by accounts.
            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);

            Participant participant = await this.GetParticipant(accountId, channelId);
            if (participant != null && participant.IsDeleted)
            {
                (int aff, long _) = await executor.Execute(
                    ParticipantQueries.ParticipantUpdateSoftDelete,
                    new Dictionary<string, object>
                    {
                        { "@deleted", 0 },
                        { "@accountId", accountId.ToString() },
                        { "@channelId", channelId.ToString() },
                    });

                if (aff > 0)
                {
                    this.logger.LogDebug("Link between {0} and {1} is re-linked", accountId, channelId);
                }

                return participant.Id;
            }
            else
            {
                (int aff, long id) = await executor.Execute(
                    ParticipantQueries.ParticipantCreate, // Without message info
                    new Dictionary<string, object>
                    {
                        { "@accountId", accountId.ToString() },
                        { "@channelId", channelId.ToString() },
                    });

                if (aff > 0)
                {
                    this.logger.LogDebug("New participant record created with ID = '{0}'", id);
                    return (int)id;
                }
                else
                {
                    this.logger.LogDebug("No new participant record created...");
                    return 0;
                }
            }
        }

        public Task UnlinkAccountWithChannel(AccountId accountId, ChannelId channelId)
        {
            return this.SoftDeleteParticipant(accountId, channelId);
        }

        public async Task<IList<Participant>> ListAccountParticipants(AccountId accountId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            IList<Participant> result = await executor.QueryAll<Participant>(
                ParticipantQueries.ParticipantQueryByAccountId,
                new Dictionary<string, object> { { "@accountId", accountId.ToString() } });

            return result.Where(p => !p.IsDeleted).ToArray();
        }

        public async Task<IList<Participant>> ListAccountParticipantsWithMessageInfo(AccountId accountId, long dtSince)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            IList<Participant> result = await executor.QueryAll<Participant>(
                ParticipantQueries.ParticipantQueryFullByAccountIdAndLastMsgDt,
                new Dictionary<string, object>
                {
                    {"@accountId", accountId.ToString() },
                    {"@lastMsgDt", dtSince },
                });

            return result.Where(r => !r.IsDeleted).ToList();
        }

        public async Task<bool> UpdateParticipantWithNewMessage(AccountId accountId, ChannelId channelId, Message message)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.Equals(message.SenderAccountId.Name, accountId.Name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Sending message to the same account is not allowed.");
            }

            ParticipantMessageInfo messageInfo = new ParticipantMessageInfo
            {
                MessageFirst100Chars = message.GetFirst100MessageChars(),
                MessageId = message.Uuid,
                SenderAccountId = message.SenderAccountId,
            };

            // 0. Try get the participant
            Participant participant = await this.GetParticipant(accountId, channelId);
            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            string messageInfoJson = JsonConvert.SerializeObject(messageInfo);
            long messageDt = message.MessageTsDt;
            if (participant == null)
            {
                // 1. Participant not exist, try link it.
                (int aff, long id) = await executor.Execute(
                    ParticipantQueries.ParticipantCreateWithMessage, // With message info
                    new Dictionary<string, object>
                    {
                        { "@accountId", accountId.ToString() },
                        { "@channelId", channelId.ToString() },
                        { "@messageInfo", messageInfoJson },
                        { "@lastMsgDt", messageDt }
                    });

                if (aff > 0)
                {
                    this.logger.LogDebug("New participant record created with ID = '{0}' w/ message", id);
                }

                return aff > 0;
            }
            else
            {
                if (participant.LastMessageDt < messageDt)
                {
                    // This message is latest.
                    (int aff, long _) = await executor.Execute(
                        ParticipantQueries.ParticipantUpdateMessageInfo,
                        new Dictionary<string, object>
                        {
                            { "@messageInfo", messageInfoJson },
                            { "@lastMsgDt", messageDt },
                            { "@id", participant.Id },
                            { "@version", participant.Version }

                        });

                    return aff > 0;
                }
                else
                {
                    // This message is not latest, just update message count.
                    (int aff, long _) = await executor.Execute(
                        ParticipantQueries.ParticipantUpdateMessageCount,
                        new Dictionary<string, object>
                        {
                            { "@id", participant.Id },
                            { "@version", participant.Version }
                        });

                    return aff > 0;
                }
            }
        }

        public async Task<bool> UpdateParticipantLastReadMessageCount(AccountId accountId, ChannelId channelId, int messageCount)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            Participant participant = await this.GetParticipant(accountId, channelId);
            if (participant == null || participant.IsDeleted)
            {
                throw new NotFound($"Cannot find participant of {accountId}@{channelId}");
            }

            if (participant.MessageCount < messageCount || participant.MessageRead > messageCount)
            {
                throw new Forbidden($"Message count forbids this update : Count {participant.MessageCount}, Read {participant.MessageRead}");
            }

            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);

            (int aff, long _) = await executor.Execute(
                ParticipantQueries.ParticipantUpdateLastReadCount,
                new Dictionary<string, object>
                {
                    { "@read", messageCount },
                    { "@id", participant.Id },
                    { "@version", participant.Version }
                });

            return aff > 0;
        }

        private async Task SoftDeleteParticipant(AccountId accountId, ChannelId channelId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            int partition = await this.accountService.GetUserAccountPartition(accountId);
            MySqlExecutor executor = this.partitionProvider.GetDataExecutor(partition);
            (int aff, long _) = await executor.Execute(
                ParticipantQueries.ParticipantUpdateSoftDelete,
                new Dictionary<string, object>
                {
                    { "@deleted", 1 },
                    { "@accountId", accountId.ToString() },
                    { "@channelId", channelId.ToString() }
                });

            if (aff > 0)
            {
                this.logger.LogDebug($"Link between {accountId} and {channelId} is soft deleted");
            }
            else
            {
                throw new NotFound($"Link not found between {accountId} and {channelId}");
            }
        }

        private async Task<Participant> GetParticipant(AccountId accountId, ChannelId channelId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            int partition = await this.accountService.GetUserAccountPartition(accountId);
            Participant result = (await this.partitionProvider.GetDataExecutor(partition).QueryAll<Participant>(
                ParticipantQueries.ParticipantQueryByIds,
                new Dictionary<string, object>
                {
                    { "@accountId", accountId.ToString() },
                    { "@channelId", channelId.ToString() }
                })).FirstOrDefault();

            return result;
        }
    }
}