namespace ChatChan.Service.Model
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;

    public enum MessageType : uint
    {
        Text = 0,
    }

    public class Message : ISqlRecord
    {
        public long Id { get; set; }

        public MessageType Type { get; set; }

        public string Uuid { get; set; }

        public AccountId SenderAccountId { get; set; }

        public ChannelId ChannelId { get; set; }

        public string MessageBody { get; set; }

        public long OrdinalNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string GetFirst100MessageChars()
        {
            if (string.IsNullOrEmpty(this.MessageBody))
            {
                return null;
            }
            else if (this.MessageBody.Length > 100)
            {
                return this.MessageBody.Substring(0, 100);
            }
            else
            {
                return this.MessageBody;
            }
        }

        public Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt64);
            this.Type = (MessageType)reader.ReadColumn(nameof(this.Type), reader.GetInt32);
            this.Uuid = reader.ReadColumn(nameof(this.Uuid), reader.GetString);
            string senderAccountId = reader.ReadColumn("SenderActId", reader.GetString);
            if (!AccountId.TryParse(senderAccountId, out AccountId senderActIdObj))
            {
                throw new DataException($"Unexpected data format for account ID for {this.Id}");
            }

            this.SenderAccountId = senderActIdObj;

            string channelId = reader.ReadColumn(nameof(this.ChannelId), reader.GetString);
            if (!string.IsNullOrEmpty(channelId))
            {
                if (!ChannelId.TryParse(channelId, out ChannelId channelIdObj))
                {
                    throw new DataException($"Unexpected data format for channel ID for {this.Id}");
                }

                this.ChannelId = channelIdObj;
            }

            this.MessageBody = reader.ReadColumn(nameof(this.MessageBody), reader.GetString);
            this.OrdinalNumber = reader.ReadColumn(nameof(this.OrdinalNumber), reader.GetInt64);
            this.CreatedAt = reader.ReadDateColumn(nameof(this.CreatedAt));

            return Task.FromResult(0);
        }
    }
}
