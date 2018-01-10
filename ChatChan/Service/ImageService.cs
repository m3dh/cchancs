namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;
    using ChatChan.Provider.StoreModel;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface IImageService
    {
        Task<Guid> CreateCoreImage(string type, byte[] imageData);
        Task<CoreImage> GetCoreImage(Guid imageGuid);
    }

    public class ImageService : IImageService
    {
        private readonly MySqlExecutor sqlExecutor;
        private readonly ILogger logger;

        public ImageService(ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection)
        {
            this.logger = loggerFactory.CreateLogger<ImageService>();
            this.sqlExecutor = new MySqlExecutor(
                storageSection?.Value?.CoreDatabase ?? throw new ArgumentNullException(nameof(storageSection)),
                loggerFactory);
        }

        public async Task<Guid> CreateCoreImage(string type, byte[] imageData)
        {
            Guid imageGuid = Guid.NewGuid();
            (int affect, long id) = await this.sqlExecutor.Execute(Queries.ImageCreation, new Dictionary<string, object>
            {
                { "@uuid", imageGuid.ToString("N") },
                { "@data", imageData },
                { "@type", type }
            });

            this.logger.LogDebug($"New image created with 'affect' = {affect}, 'id' = {id}");
            return imageGuid;
        }

        public async Task<CoreImage> GetCoreImage(Guid imageGuid)
        {
            this.logger.LogInformation("UUID >>> {0}", imageGuid.ToString("N"));
            CoreImage image = 
                (await this.sqlExecutor.QueryAll<CoreImage>(Queries.ImageQueryByUuid, new Dictionary<string, object> { { "@uuid", imageGuid.ToString("N") } }))
                .SingleOrDefault();

            if (image == null || image.Data == null)
            {
                throw new NotFoundException($"Image with UUID {imageGuid} is not found");
            }

            return image;
        }
    }
}
