﻿namespace ChatChan.Service.Model
{
    using System;
    using System.Data.Common;
    using System.Threading.Tasks;

    using ChatChan.Provider.Executor;
    using ChatChan.Service.Identifier;

    public class BaseImage : ISqlRecord
    {
        public int Id { get; set; }

        public ImageId ImageId { get; set; }

        public Guid Uuid { get; set; }

        public string ContentType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Task Fill(DbDataReader reader)
        {
            this.CreatedAt = reader.ReadDateColumn(nameof(this.CreatedAt));
            this.ContentType = reader.ReadColumn("Type", reader.GetString);
            this.IsDeleted = reader.ReadColumn(nameof(this.IsDeleted), reader.GetBoolean);

            return Task.FromResult(0);
        }
    }

    public class CoreImage : BaseImage
    {
        public byte[] Data { get; set; }

        public override Task Fill(DbDataReader reader)
        {
            this.Data = (byte[])reader[nameof(this.Data)];
            return base.Fill(reader);
        }
    }
}