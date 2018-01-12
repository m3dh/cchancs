namespace ChatChan.Service.Model
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;

    public class UserClientToken : ISqlRecord
    {
        public int Id { get; set; }

        public string AccountName { get; set; }

        public int DeviceId { get; set; }

        public string Token { get; set; }

        public DateTimeOffset LastGetAt { get; set; }

        public DateTimeOffset ExpiredAt { get; set; }

        public int Version { get; set; }

        public Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            this.AccountName = reader.ReadColumn(nameof(this.AccountName), reader.GetString);
            this.DeviceId = reader.ReadColumn(nameof(this.DeviceId), reader.GetInt32);
            this.Token = reader.ReadColumn(nameof(this.Token), reader.GetString);
            this.LastGetAt = reader.ReadColumn(nameof(this.LastGetAt), reader.GetDateTime);
            this.ExpiredAt = reader.ReadColumn(nameof(this.ExpiredAt), reader.GetDateTime);
            this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);

            return Task.FromResult(0);
        }
    }
}