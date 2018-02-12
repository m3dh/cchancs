namespace ChatChan.Tests.Functional
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using ChatChan.Common;
    using ChatChan.Controller;

    using Newtonsoft.Json;

    using Xunit;

    public class ChatClient
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        public readonly string AccountId;
        private readonly string token;
        private readonly int deviceId;

        public static UserAccountViewModel CreateUserAccount(string accountName, string displayName)
        {
            UserAccountInputModel request = new UserAccountInputModel
            {
                AccountName = accountName,
                DisplayName = displayName,
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(request));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage resp = HttpClient.PostAsync($"{GlobalHelper.TestServer}/api/accounts/users", content).Result;
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            UserAccountViewModel response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
            return response;
        }

        public static UserAccountViewModel SetUserAccountPassword(string accountName, string password)
        {
            UserAccountInputModel request = new UserAccountInputModel
            {
                Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(password)),
            };

            HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("POST"), $"{GlobalHelper.TestServer}/api/accounts/users/{accountName}/password");
            req.Content = new StringContent(JsonConvert.SerializeObject(request));
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage resp = HttpClient.SendAsync(req).Result;
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            UserAccountViewModel response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
            return response;
        }

        public static DeviceTokenViewModel LogonAccount(string accountName, string password)
        {
            UserAccountInputModel request = new UserAccountInputModel
            {
                Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(password)),
            };

            HttpRequestMessage req0 = new HttpRequestMessage(new HttpMethod("POST"), $"{GlobalHelper.TestServer}/api/accounts/users/{accountName}/tokens");
            req0.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
            req0.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req0.Content.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage resp0 = HttpClient.SendAsync(req0).Result;

            var responseString = resp0.Content.ReadAsStringAsync().Result;
            Assert.Equal(HttpStatusCode.Created, resp0.StatusCode);
            DeviceTokenViewModel token0 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(responseString);
            return token0;
        }

        public ChatClient(string accountId, DeviceTokenViewModel token)
        {
            this.AccountId = accountId;
            this.deviceId = token.DeviceId;
            this.token = token.Token;
        }

        public GeneralChannelViewModel CreateDirectMessageChannel(string secondAccountId)
        {
            DirectMessageChannelInputModel request = new DirectMessageChannelInputModel
            {
                SourceAccountId = this.AccountId,
                TargetAccountId = secondAccountId,
            };

            return this.Post<GeneralChannelViewModel>($"{GlobalHelper.TestServer}/api/channels/dms", request);
        }

        public List<GeneralChannelViewModel> ListMyChannels()
        {
            return this.Get<List<GeneralChannelViewModel>>($"{GlobalHelper.TestServer}/api/channels?accounId={this.AccountId}");
        }

        public GeneralChannelViewModel GetChannel(string channelId)
        {
            return this.Get<GeneralChannelViewModel>($"{GlobalHelper.TestServer}/api/channels/{channelId}");
        }

        public GeneralMessageViewModel PostNewChannelMessage(string channelId)
        {
            PostTextMessageInputModel input = new PostTextMessageInputModel
            {
                Message = $"Some text message @ {DateTimeOffset.UtcNow:F}",
                SenderAccountId = this.AccountId,
                Uuid = Guid.NewGuid().ToString("N")
            };

            return this.Post<GeneralMessageViewModel>($"{GlobalHelper.TestServer}/api/channels/{channelId}/textMessages", input);
        }

        public List<GeneralMessageViewModel> ListMessagesByChannel(string channelId, long lastMsgOrdinalNumber = 0)
        {
            return this.Get<List<GeneralMessageViewModel>>($"{GlobalHelper.TestServer}/api/channels/{channelId}/messages?lastMsgOrdinalNumber={lastMsgOrdinalNumber}");
        }

        public List<ParticipantViewModel> ListMyParticipants(long prevUpdatedDt)
        {
            return this.Get<List<ParticipantViewModel>>($"{GlobalHelper.TestServer}/api/participants?prevUpdatedDt={prevUpdatedDt}&accountId={this.AccountId}");
        }

        private TRet Get<TRet>(string url)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
            this.AuthHeaders(req.Headers);

            HttpResponseMessage resp = HttpClient.SendAsync(req).Result;
            string respString = resp.Content.ReadAsStringAsync().Result;
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            return JsonConvert.DeserializeObject<TRet>(respString);
        }

        private TRet Post<TRet>(string url, object body)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body)),
            };

            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            this.AuthHeaders(req.Content.Headers);
            HttpResponseMessage resp = HttpClient.SendAsync(req).Result;
            string respString = resp.Content.ReadAsStringAsync().Result;
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            return JsonConvert.DeserializeObject<TRet>(respString);
        }

        private void AuthHeaders(HttpHeaders headers)
        {
            headers.Add(Constants.UserHeaderName, new[] { this.AccountId });
            headers.Add(Constants.TokenHeaderName, new[] { $"{this.deviceId}:{this.token}" });
        }
    }
}
