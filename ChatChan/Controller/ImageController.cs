namespace ChatChan.Controller
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Middleware;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.AspNetCore.Mvc;

    using Newtonsoft.Json;

    public class ImageViewModel
    {
        [JsonProperty(PropertyName = "image_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "content_type")]
        public string ContentType { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty(PropertyName = "url")]
        public Uri Uri { get; set; }
    }

    public class ImageController : Controller
    {
        private static readonly string[] AcceptableImageContentTypes = new[] { "image/jpeg" };
        private readonly IImageService imageService;

        public ImageController(IImageService imageService)
        {
            this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        }

        [HttpPost, Route("api/images/avatars")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<ImageViewModel> CreateAvatarImage()
        {
            if (null == this.Request.Body)
            {
                throw new BadRequest("Body");
            }

            if (this.Request.ContentLength == null || this.Request.ContentLength.Value <= 0)
            {
                throw new BadRequest("Content-Length");
            }

            if (string.IsNullOrEmpty(this.Request.ContentType)
                || AcceptableImageContentTypes.All(t => !string.Equals(t, this.Request.ContentType, StringComparison.Ordinal)))
            {
                throw new BadRequest("Content-Type");
            }

            // Since we want to resize the images, have to read the full content here.
            byte[] imageData;
            using (MemoryStream ms = new MemoryStream())
            {
                // TODO [P2] : add image size protector, and per user upload quota / throttler.
                await this.Request.Body.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            // Parse conent type.
            string imageContentType = this.Request.ContentType.Split("/")[1];
            ImageId avatarImageId = await this.imageService.CreateCoreImage(imageContentType, imageData);
            return new ImageViewModel
            {
                Id = avatarImageId.ToString(),
                ContentType = imageContentType,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        [HttpGet, Route("api/images/{imageId}")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<ImageViewModel> GetImageMetadata(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
            {
                throw new BadRequest(nameof(imageId));
            }

            if (!ImageId.TryParse(imageId, out ImageId imageIdObj))
            {
                throw new BadRequest(nameof(imageId));
            }

            BaseImage image = await this.imageService.GetImage(imageIdObj);
            return new ImageViewModel
            {
                Id = imageId,
                ContentType = image.ContentType,
                CreatedAt = image.CreatedAt,
                Uri = ImageController.GetImageUri(image.ImageId),
            };
        }

        [HttpGet, Route("api/images/core/{imageId}")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<ActionResult> GetCoreImageData(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
            {
                throw new BadRequest(nameof(imageId));
            }

            if (!ImageId.TryParse(imageId, out ImageId imageIdObj))
            {
                throw new BadRequest(nameof(imageId));
            }

            CoreImage image = await this.imageService.GetCoreImage(imageIdObj);
            return new FileContentResult(image.Data, $"image/{image.ContentType}");
        }

        public static Uri GetImageUri(ImageId imageId)
        {
            switch (imageId.Type)
            {
                case ImageId.ImageType.CI:
                    return null;

                default:
                    throw new InvalidOperationException($"Unsupported image type : {imageId.Type}");
            }
        }
    }
}