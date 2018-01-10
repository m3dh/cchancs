namespace ChatChan.Provider.StoreModel
{
    using System;
    using System.Data.Common;
    using System.IO;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;

    public class CoreImage : ISqlRecord
    {
        public int Id { get; set; }

        public Guid Uuid { get; set; }

        public string Type { get; set; }

        public byte[] Data { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Task Fill(DbDataReader reader)
        {
            this.CreatedAt = reader.ReadColumn(nameof(this.CreatedAt), reader.GetDateTime);
            this.Data = (byte[])reader[nameof(this.Data)];
            this.Type = reader.ReadColumn(nameof(this.Type), reader.GetString);

            return Task.FromResult(0);
        }
    }
}
