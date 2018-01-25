namespace ChatChan.Service.Model
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Threading.Tasks;
    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;
    using Newtonsoft.Json;

    public class Channel : ISqlRecord
    {
        public int Id { get; set; }

        public ChannelId.ChannelType Type { get; set; }

        public int Partition { get; set; }

        public long Status { get; set; }

        public string DisplayName { get; set; }

        public AccountId OwnerAccountId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int Version { get; set; }

        public ChannelId ChannelId
        {
            get { return new ChannelId { Id = this.Id, Type = this.Type }; }
        }

        public Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            this.Type = (ChannelId.ChannelType)reader.ReadColumn(nameof(this.Type), reader.GetInt32);
            this.Partition = reader.ReadColumn(nameof(this.Partition), reader.GetInt32);
            this.Status = reader.ReadColumn(nameof(this.Status), reader.GetInt64);
            this.DisplayName = reader.ReadColumn(nameof(this.DisplayName), reader.GetString);
            this.CreatedAt = reader.ReadDateColumn(nameof(this.CreatedAt));
            this.UpdatedAt = reader.ReadDateColumn(nameof(this.UpdatedAt));
            this.IsDeleted = reader.ReadColumn(nameof(this.IsDeleted), reader.GetBoolean);
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);

            return Task.FromResult(0);
        }
    }

    public class ChannelMemberList : ISqlRecord
    {
        public int Id { get; set; }

        public ChannelId.ChannelType Type { get; set; }

        public IList<AccountId> MemberList { get; set; }

        public long Status { get; set; }

        public bool IsDeleted { get; set; }

        public int Version { get; set; }

        public Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            this.Type = (ChannelId.ChannelType)reader.ReadColumn(nameof(this.Type), reader.GetInt32);
            string membersJson = reader.ReadColumn(nameof(this.MemberList), reader.GetString);
            this.MemberList = JsonConvert.DeserializeObject<List<AccountId>>(membersJson);
            this.Status = reader.ReadColumn(nameof(this.Status), reader.GetInt64);
            this.IsDeleted = reader.ReadColumn(nameof(this.IsDeleted), reader.GetBoolean);
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);

            return Task.FromResult(0);
        }
    }
}
