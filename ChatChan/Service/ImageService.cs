namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    public interface IImageService
    {
        Task<ImageId> CreateCoreImage(string type, byte[] imageData);
        Task<CoreImage> GetCoreImage(ImageId imageId);
        Task<BaseImage> GetImage(ImageId imageId);
    }

    public class ImageService : IImageService
    {
        private readonly CoreDbProvider coreDb;
        private readonly ILogger logger;

        public ImageService(ILoggerFactory loggerFactory, CoreDbProvider coreDb)
        {
            this.logger = loggerFactory.CreateLogger<ImageService>();
            this.coreDb = coreDb;
        }

        public async Task<ImageId> CreateCoreImage(string type, byte[] imageData)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentNullException(nameof(imageData));
            }

            Guid imageGuid = Guid.NewGuid();
            (int affect, long id) = await this.coreDb.Execute(ImageQueries.CoreImageCreation, new Dictionary<string, object>
            {
                { "@uuid", imageGuid.ToString("N") },
                { "@data", imageData },
                { "@type", type }
            });

            this.logger.LogDebug($"New image created with 'affect' = {affect}, 'id' = {id}");
            return new ImageId { Guid = imageGuid, Type = ImageId.ImageType.CI };
        }

        public async Task<BaseImage> GetImage(ImageId imageId)
        {
            BaseImage ret;
            switch (imageId.Type)
            {
                case ImageId.ImageType.CI:
                    ret = await this.GetCoreImageMetadata(imageId.Guid);
                    break;

                default:
                    throw new BadRequest(nameof(imageId.Type), imageId.Type.ToString());
            }

            if (ret == null || ret.IsDeleted)
            {
                throw new NotFound($"Image with ID {imageId} is not found");
            }

            ret.ImageId = imageId;
            return ret;
        }

        public async Task<CoreImage> GetCoreImage(ImageId imageId)
        {
            if (imageId == null)
            {
                throw new ArgumentNullException(nameof(imageId));
            }

            if (imageId.Type != ImageId.ImageType.CI)
            {
                throw new BadRequest(nameof(imageId.Type), imageId.Type.ToString());
            }

            CoreImage image =
                (await this.coreDb.QueryAll<CoreImage>(ImageQueries.CoreImageQueryByUuid, new Dictionary<string, object> { { "@uuid", imageId.Guid.ToString("N") } }))
                .SingleOrDefault();

            if (image == null || image.Data == null || image.IsDeleted)
            {
                throw new NotFound($"Core image with ID {imageId} is not found");
            }

            image.ImageId = imageId;
            return image;
        }

        private async Task<BaseImage> GetCoreImageMetadata(Guid coreImageGuid)
        {
            return (await this.coreDb.QueryAll<BaseImage>(ImageQueries.CoreImageMetaQueryByUuid,
                    new Dictionary<string, object> { { "@uuid", coreImageGuid.ToString("N") } }))
                .SingleOrDefault();
        }
    }
}