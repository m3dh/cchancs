namespace ChatChan.Service
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using ChatChan.Provider;

    public class ImageService
    {
        private readonly MySqlProvider dbProvider;

        public ImageService(MySqlProvider dbProvider)
        {
            this.dbProvider = dbProvider;
        }

        public async Task<string> CreateImage(MemoryStream stream)
        {
            if(stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            byte[] bytes = stream.ToArray();
            return await this.dbProvider.Execute<string>("");
        }
    }
}
