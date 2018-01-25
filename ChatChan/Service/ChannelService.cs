namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    public interface IChannelService
    {
        Task<int> CountChannel();

        Task<Channel> GetChannel(ChannelId channelId);
        Task<int> GetChannelParititon(ChannelId channelId);
        Task<ChannelId> CreateDirectMessageChannel(AccountId from, AccountId to, string displayName);
        Task DeleteChannel(ChannelId channelId);
    }

    public class ChannelService : IChannelService
    {
        private readonly ILogger logger;
        private readonly CoreDbProvider coreDb;
        private readonly IDataPartitionProvider partitionProvider;

        [Flags]
        private enum ChannelStatus : long
        {
            NewChannel = 0,
        }

        public ChannelService(
            ILoggerFactory loggerFactory,
            CoreDbProvider coreDb,
            IDataPartitionProvider partitionProvider)
        {
            this.logger = loggerFactory?.CreateLogger<ChannelService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.coreDb = coreDb ?? throw new ArgumentNullException(nameof(coreDb));
            this.partitionProvider = partitionProvider ?? throw new ArgumentNullException(nameof(partitionProvider));
        }

        public async Task<int> GetChannelParititon(ChannelId channelId)
        {
            Channel channel = await this.GetChannel(channelId);
            return channel.Partition;
        }

        // If the channel already exists, read it.
        public async Task<ChannelId> CreateDirectMessageChannel(AccountId source, AccountId target, string displayName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (string.Equals(source.ToString(), target.ToString(), StringComparison.Ordinal))
            {
                throw new BadRequest("Cannot create direct message for the same account.");
            }

            List<AccountId> accounts = new List<AccountId> { source, target };
            string memberListJson = JsonConvert.SerializeObject(accounts);
            string memberListHash = this.GetMemberJsonHash(memberListJson);
            string ownerAccountId = accounts.OrderBy(a => a.Name).First().ToString();

            try
            {
                int partition = this.partitionProvider.GetPartition();
                (int affected, long id) = await this.coreDb.Execute(
                    ChannelQueries.ChannelCreate,
                    new Dictionary<string, object>
                    {
                        { "@type", (int)ChannelId.ChannelType.DM },
                        { "@partition", partition },
                        { "@displayName", displayName },
                        { "@status", (long)ChannelStatus.NewChannel },
                        { "@ownerActId", ownerAccountId },
                        { "@memberList", memberListJson },
                        { "@memberHash", memberListHash }
                    });

                if (affected <= 0)
                {
                    throw new DataException("Unable to insert new channel record...");
                }

                this.logger.LogInformation($"New channel created with ID = '{id}', Partition = '{partition}'");
                return new ChannelId
                {
                    Type = ChannelId.ChannelType.DM,
                    Id = (int)id
                };
            }
            catch (Conflict conflict) when(conflict.ErrorCode == Conflict.Code.Duplication)
            {
                Channel channel = (await this.coreDb.QueryAll<Channel>(
                    ChannelQueries.ChannelQueryByOwnerAndMemberHash,
                    new Dictionary<string, object>
                    {
                        { "@ownerId", ownerAccountId },
                        { "@memberHash", memberListHash }
                    })).FirstOrDefault();

                if (channel == null)
                {
                    throw new DataException($"Unable to find channel under account {ownerAccountId}");
                }

                if (channel.IsDeleted) // & Status allow activation.
                {
                    await this.UpdateChannelSoftDelete(channel.Id, false);
                }

                return channel.ChannelId;
            }
        }

        public Task DeleteChannel(ChannelId channelId)
        {
            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            return this.UpdateChannelSoftDelete(channelId.Id, false);
        }

        public Task<int> CountChannel()
        {
            return this.coreDb.QueryScalar<int>(ChannelQueries.ChannelCountRecords);
        }

        public async Task<Channel> GetChannel(ChannelId channelId)
        {
            if (channelId == null)
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            // Group and DM channels all stored in this table.
            Channel channel = (await this.coreDb.QueryAll<Channel>(
                ChannelQueries.ChannelQueryById,
                new Dictionary<string, object> { { "@id", channelId.Id } })).FirstOrDefault();

            if (channel == null || channel.IsDeleted)
            {
                throw new NotFound($"Channel ID = '{channelId}' is not found");
            }

            return channel;
        }

        private async Task UpdateChannelSoftDelete(int id, bool deleted)
        {
            (int aff, long _) = await this.coreDb.Execute(
                ChannelQueries.ChannelUpdateSoftDelete,
                new Dictionary<string, object>
                {
                    { "@deleted", deleted ? 1 : 0 },
                    { "@id", id }
                });

            if (aff > 0)
            {
                this.logger.LogDebug("Channel {0} set deleted = {1}", id, deleted);
            }
        }

        private string GetMemberJsonHash(string memberJson)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(memberJson);
                    memory.Write(inputBytes, 0, inputBytes.Length);
                    using (SHA256 hash = new SHA256Managed())
                    {
                        return Convert.ToBase64String(hash.ComputeHash(memory.ToArray()));
                    }
                }
            }
            catch (FormatException ex)
            {
                this.logger.LogInformation("Members list encoding failed with : {0}", ex.Message);
                throw new BadRequest(nameof(memberJson));
            }
        }
    }
}
