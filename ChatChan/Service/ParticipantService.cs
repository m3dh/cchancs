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

    public interface IParticipantService
    {
        // Participants are not guaranteed to sync with channel's members, so this is a delayed result.
        Task<IList<Participant>> ListAccountParticipants(AccountId accountId);
        Task<int> LinkAccountWithChannel(AccountId accountId, ChannelId channelId);
        Task UnlinkAccountWithChannel(AccountId accountId, ChannelId channelId);
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
                        { "@deleted", 1 },
                        { "@accountId", accountId.ToString() },
                        { "@channelId", channelId.ToString() }
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
                    ParticipantQueries.ParticipantCreate,
                    new Dictionary<string, object>
                    {
                        { "@accountId", accountId.ToString() },
                        { "@channelId", channelId.ToString() }
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