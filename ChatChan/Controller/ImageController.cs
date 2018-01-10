namespace ChatChan.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Provider.StoreModel;
    using ChatChan.Service;
    using Microsoft.AspNetCore.Mvc;

    public class ImageController : Controller
    {
        private static readonly string[] AcceptableImageContentTypes = new[] { "image/jpeg" };
        private readonly IImageService imageService;

        public ImageController(IImageService imageService)
        {
            this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        }

        [HttpPost, Route("api/images/avatars")]
        public async Task<Dictionary<string,string>> CreateAvatarImage()
        {
            if (null == this.Request.Body)
            {
                throw new ClientInputException("Body");
            }

            if (this.Request.ContentLength == null || this.Request.ContentLength.Value <= 0)
            {
                throw new ClientInputException("Content-Length");
            }

            if (string.IsNullOrEmpty(this.Request.ContentType)
                || AcceptableImageContentTypes.All(t => !string.Equals(t, this.Request.ContentType, StringComparison.Ordinal)))
            {
                throw new ClientInputException("Content-Type");
            }

            // Since we want to resize the images, have to read the full content here.
            byte[] imageData;
            using(MemoryStream ms = new MemoryStream())
            {
                // TODO [P2] : add image size protector, and per user upload quota / throttler.
                await this.Request.Body.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            // Parse conent type.
            string imageType = this.Request.ContentType.Split("/")[1];
            Guid avatarImageId = await this.imageService.CreateCoreImage(imageType, imageData);
            return new Dictionary<string, string> { { "uuid", avatarImageId.ToString("N") } };
        }

        [HttpGet, Route("api/images/avatars/{uuid:guid}")]
        public async Task<ActionResult> GetAvatarImage(Guid uuid)
        {
            CoreImage image = await this.imageService.GetCoreImage(uuid);
            return new FileContentResult(image.Data, $"image/{image.Type}");
        }
    }
}
