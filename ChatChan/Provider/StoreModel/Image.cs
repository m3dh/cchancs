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

        public async Task Fill(DbDataReader reader)
        {
            this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt32);
            this.Uuid = reader.ReadColumn(nameof(this.Uuid), reader.GetGuid);
            this.CreatedAt = reader.ReadColumn(nameof(this.CreatedAt), reader.GetDateTime);
            this.Type = reader.ReadColumn(nameof(this.Type), reader.GetString);

            using (Stream dataStream = reader.ReadColumn(nameof(this.Data), reader.GetStream))
            using (MemoryStream outStream = new MemoryStream())
            {
                await dataStream.CopyToAsync(outStream);
                this.Data = outStream.ToArray();
            }
        }
    }
}
