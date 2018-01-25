namespace ChatChan.Tests.Functional
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using ChatChan.Common;
    using ChatChan.Controller;
    using Newtonsoft.Json;

    public class ChatAppAuthProvider
    {
        private static ChatAppAuthProvider instance;

        private readonly string accountId;
        private readonly string token;
        private readonly int deviceId;

        private ChatAppAuthProvider()
        {
            string accountName = $"auth-account-{(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds}";
            using (HttpClient client = new HttpClient())
            {
                // Create the account and parse response.
                UserAccountInputModel request = new UserAccountInputModel
                {
                    AccountName = accountName,
                    DisplayName = "Test Account for Auth",
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp = client.PostAsync("http://localhost:8080/api/accounts/users", content).Result;
                Debug.Assert(HttpStatusCode.Created == resp.StatusCode);
                UserAccountViewModel response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);

                // Now set account password
                request = new UserAccountInputModel
                {
                    Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("CHATCHAN_APP")),
                };

                HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/password");
                req.Content = new StringContent(JsonConvert.SerializeObject(request));
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req.Content.Headers.ContentType.CharSet = "utf-8";
                resp = client.SendAsync(req).Result;
                var err = resp.Content.ReadAsStringAsync().Result;
                Debug.Assert(HttpStatusCode.Created == resp.StatusCode);

                // Logon.
                HttpRequestMessage req0 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
                req0.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req0.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req0.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp0 = client.SendAsync(req0).Result;
                Debug.Assert(HttpStatusCode.Created == resp0.StatusCode);
                DeviceTokenViewModel token0 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp0.Content.ReadAsStringAsync().Result);

                this.accountId = response.Id;
                this.deviceId = token0.DeviceId;
                this.token = token0.Token;
            }
        }

        public ChatAppAuthProvider(string accountId, int device, string token)
        {
            this.accountId = accountId;
            this.deviceId = device;
            this.token = token;
        }

        public void AuthIt(HttpHeaders headers)
        {
            headers.Add(Constants.UserHeaderName, new[] { this.accountId });
            headers.Add(Constants.TokenHeaderName, new[] { $"{this.deviceId}:{this.token}" });
        }

        public static ChatAppAuthProvider Instace
        {
            get { return instance ?? (instance = new ChatAppAuthProvider()); }
        }
    }
}
