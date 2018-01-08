namespace ChatChan.Controller
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    [Route("api/images")]
    public class ImageController : Controller
    {
        [HttpPost]
        public async Task<string> CreateImage()
        {
            if (null == Request.Body)
            {
                throw new ArgumentNullException("Create image request body is null or empty.");
            }

            // Since we want to resize the images, have to read the full content here.
            // TODO [P2] : add image size protector, and per user upload quota / throttler.
            using(MemoryStream ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
            }

            return "";
        }
    }
}
