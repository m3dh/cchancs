namespace ChatChan.Tests.Functional
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Newtonsoft.Json;
    using Xunit;

    public class ImageServiceTests
    {
        [Fact]
        public void CreateAndDownloadAvatarImages_ShouldMatch()
        {
            // Create a image by downloading...
            using (HttpClient client = new HttpClient())
            {
                byte[] image = client
                    .GetAsync("http://car3.autoimg.cn/cardfs/product/g1/M05/17/D6/t_autohomecar__wKjB1lo1NcSAEeiEAAjQX3JDJSA938.jpg")
                    .Result
                    .Content
                    .ReadAsByteArrayAsync()
                    .Result;

                var content = new ByteArrayContent(image);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                var resp = client.PostAsync("http://localhost:8080/api/images/avatars", content).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp.Content.ReadAsStringAsync().Result);
                var id = result["uuid"].ToString();

                resp = client.GetAsync($"http://localhost:8080/api/images/avatars/{id}").Result;
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(image.Length, resp.Content.Headers.ContentLength);
                Assert.Equal("image/jpeg", resp.Content.Headers.ContentType.MediaType);
                Assert.Equal(image, resp.Content.ReadAsByteArrayAsync().Result);
            }
        }
    }
}
