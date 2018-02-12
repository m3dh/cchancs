namespace ChatChan.Tests.Functional
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using ChatChan.Controller;

    using Newtonsoft.Json;

    using Xunit;

    public class ImageServiceTests
    {
        [Fact]
        public void CreateAndDownloadAvatarImages_ShouldMatch()
        {
            using (HttpClient client = new HttpClient())
            {
                // Create a image by downloading...
                byte[] image = client
                    .GetAsync("http://car3.autoimg.cn/cardfs/product/g1/M05/17/D6/t_autohomecar__wKjB1lo1NcSAEeiEAAjQX3JDJSA938.jpg")
                    .Result
                    .Content
                    .ReadAsByteArrayAsync()
                    .Result;

                ByteArrayContent content = new ByteArrayContent(image);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                ChatAppAuthProvider.Instace.AuthIt(content.Headers); // Auth request
                HttpResponseMessage resp = client.PostAsync($"{GlobalHelper.TestServer}/api/images/avatars", content).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp.Content.ReadAsStringAsync().Result);
                string id = result["image_id"].ToString();
                Assert.StartsWith("CI:", id);

                // Get the image metadata
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{GlobalHelper.TestServer}/api/images/{id}");
                ChatAppAuthProvider.Instace.AuthIt(request.Headers); // Auth request
                resp = client.SendAsync(request).Result;

                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                ImageViewModel respObj = JsonConvert.DeserializeObject<ImageViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal("jpeg", respObj.ContentType);
                Assert.Equal(id, respObj.Id);

                // Get the image content...
                HttpRequestMessage request1 = new HttpRequestMessage(HttpMethod.Get, $"{GlobalHelper.TestServer}/api/images/core/{id}");
                ChatAppAuthProvider.Instace.AuthIt(request1.Headers); // Auth request
                resp = client.SendAsync(request1).Result;

                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(image.Length, resp.Content.Headers.ContentLength);
                Assert.Equal("image/jpeg", resp.Content.Headers.ContentType.MediaType);
                Assert.Equal(image, resp.Content.ReadAsByteArrayAsync().Result);
            }
        }
    }
}
