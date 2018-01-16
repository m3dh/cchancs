namespace ChatChan.Service.Model
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;

    public abstract class BaseAccount : ISqlRecord
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public long Status { get; set; }

        public ImageId Avatar { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public int Version { get; set; }

        public abstract AccountId AccountId { get; }

        public virtual Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            this.DisplayName = reader.ReadColumn(nameof(this.DisplayName), reader.GetString);
            this.Status = reader.ReadColumn(nameof(this.Status), reader.GetInt64);
            this.CreatedAt = reader.ReadColumn(nameof(this.CreatedAt), reader.GetDateTime);
            this.UpdatedAt = reader.ReadColumn(nameof(this.UpdatedAt), reader.GetDateTime);
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);

            string avatarImageId = reader.ReadColumn(nameof(this.Avatar), reader.GetString);
            if (!string.IsNullOrEmpty(avatarImageId))
            {
                if (!ImageId.TryParse(avatarImageId, out ImageId avatarImageIdObj))
                {
                    throw new InvalidOperationException($"Unexpected avatar image ID : {avatarImageId}");
                }
                else
                {
                    this.Avatar = avatarImageIdObj;
                }
            }

            return Task.FromResult(0);
        }
    }

    public class UserAccount : BaseAccount
    {
        public string AccountName { get; set; }

        public string Password { get; set; }

        public override AccountId AccountId
        {
            get
            {
                return new AccountId
                {
                    Name = this.AccountName,
                    Type = AccountId.AccountType.UA
                };
            }
        }

        public override Task Fill(DbDataReader reader)
        {
            this.AccountName = reader.ReadColumn(nameof(this.AccountName), reader.GetString);
            this.Password = reader.ReadColumn(nameof(this.Password), reader.GetString);

            return base.Fill(reader);
        }
    }
}