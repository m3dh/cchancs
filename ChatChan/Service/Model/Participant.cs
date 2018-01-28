namespace ChatChan.Service.Model
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;

    using Newtonsoft.Json;

    public class Participant : ISqlRecord
    {
        public int Id { get; set; }

        public AccountId AccountId { get; set; }

        public ChannelId ChannelId { get; set; }

        public ParticipantMessageInfo MessageInfo { get; set; }

        public int MessageCount { get; set; }

        public int MessageRead { get; set; }

        // Dt timestamps are UNIX millisecond created by C# code.
        public long LastMessageDt { get; set; }

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

            string messageInfoJson = reader.ReadColumn(nameof(this.MessageInfo), reader.GetString);
            if (!string.IsNullOrEmpty(messageInfoJson))
            {
                this.MessageInfo = JsonConvert.DeserializeObject<ParticipantMessageInfo>(messageInfoJson);
            }

            this.MessageCount = reader.ReadColumn(nameof(this.MessageCount), reader.GetInt32);
            this.MessageRead = reader.ReadColumn(nameof(this.MessageRead), reader.GetInt32);
            this.LastMessageDt = reader.ReadColumn(nameof(this.LastMessageDt), reader.GetInt64);
            this.CreatedAt = reader.ReadDateColumn(nameof(this.CreatedAt));
            this.UpdatedAt = reader.ReadDateColumn(nameof(this.UpdatedAt));
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);
            this.IsDeleted = reader.ReadColumn(nameof(this.IsDeleted), reader.GetBoolean);

            return Task.FromResult(0);
        }
    }

    public class ParticipantMessageInfo
    {
        [JsonProperty(PropertyName = "mid")]
        public string MessageId { get; set; }

        [JsonProperty(PropertyName = "msg100")]
        public string MessageFirst100Chars { get; set; }

        [JsonProperty(PropertyName = "sender")]
        public AccountId SenderAccountId { get; set; }
    }
}
