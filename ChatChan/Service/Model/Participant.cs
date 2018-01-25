namespace ChatChan.Service.Model
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;

    public class Participant : ISqlRecord
    {
        public int Id { get; set; }

        public AccountId AccountId { get; set; }

        public ChannelId ChannelId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int Version { get; set; }

        public Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            string accountId = reader.ReadColumn(nameof(this.AccountId), reader.GetString);
            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                throw new DataException($"Unexpected data format for account ID for {this.Id}");
            }

            this.AccountId = accountIdObj;

            string channelId = reader.ReadColumn(nameof(this.ChannelId), reader.GetString);
            if (!ChannelId.TryParse(channelId, out ChannelId channelIdObj))
            {
                throw new DataException($"Unexpected data format for channel ID for {this.Id}");
            }

            this.ChannelId = channelIdObj;

            this.CreatedAt = reader.ReadDateColumn(nameof(this.CreatedAt));
            this.UpdatedAt = reader.ReadDateColumn(nameof(this.UpdatedAt));
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);
            this.IsDeleted = reader.ReadColumn(nameof(this.IsDeleted), reader.GetBoolean);
            return Task.FromResult(0);
        }
    }
}
