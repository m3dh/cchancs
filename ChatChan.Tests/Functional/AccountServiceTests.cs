namespace ChatChan.Tests.Functional
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using ChatChan.Controller;

    using Newtonsoft.Json;

    using Xunit;

    public class AccountServiceTests
    {
        [Fact]
        public void CreateAndGetUserAccount_ShouldMatch()
        {
            this.CreateUserAccountAndVerify();
        }

        [Fact]
        public void CreateAccountThenSetPassword_ShallBeAbleToLogon()
        {
            string accountName = this.CreateUserAccountAndVerify();
            using (HttpClient client = new HttpClient())
            {
                UserAccountInputModel request = new UserAccountInputModel
                {
                    Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("CHATCHAN_APP")),
                };

                HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/password");
                req.Content = new StringContent(JsonConvert.SerializeObject(request));
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp = client.SendAsync(req).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

                // Now try to auth.
                HttpRequestMessage req0 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
                req0.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req0.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req0.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp0 = client.SendAsync(req0).Result;
                Assert.Equal(HttpStatusCode.Created, resp0.StatusCode);

                DeviceTokenViewModel token0 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp0.Content.ReadAsStringAsync().Result);
                Assert.Equal(1, token0.DeviceId);

                // If we auth once again without device
                HttpRequestMessage req1 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
                req1.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req1.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req1.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp1 = client.SendAsync(req1).Result;
                Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);

                DeviceTokenViewModel token1 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp1.Content.ReadAsStringAsync().Result);
                Assert.Equal(2, token1.DeviceId);

                // Refresh the eldest one.
                HttpRequestMessage req2 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
                req2.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req2.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req2.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp2 = client.SendAsync(req2).Result;
                Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);

                DeviceTokenViewModel token2 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp2.Content.ReadAsStringAsync().Result);
                Assert.Equal(1, token2.DeviceId);
                Assert.NotEqual(token0.Token, token2.Token);
                Assert.NotEqual(token0.ExpireAt, token2.ExpireAt);

                // Try fetch the second token again.
                HttpRequestMessage req3 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens?device_id=2");
                req3.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req3.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req3.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp3 = client.SendAsync(req3).Result;
                Assert.Equal(HttpStatusCode.Created, resp3.StatusCode);

                DeviceTokenViewModel token3 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp3.Content.ReadAsStringAsync().Result);
                Assert.Equal(2, token3.DeviceId);
                Assert.Equal(token1.Token, token3.Token);
            }
        }

        [Fact]
        public void UpdateAccount_ShouldBePersisted()
        {
            string accountName = this.CreateUserAccountAndVerify();
            using (HttpClient client = new HttpClient())
            {
                UserAccountInputModel request = new UserAccountInputModel
                {
                    Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("CHATCHAN_APP")),
                };

                HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/password");
                req.Content = new StringContent(JsonConvert.SerializeObject(request));
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp = client.SendAsync(req).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

                // Now try to auth.
                HttpRequestMessage req0 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
                req0.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
                req0.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req0.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp0 = client.SendAsync(req0).Result;
                Assert.Equal(HttpStatusCode.Created, resp0.StatusCode);
                DeviceTokenViewModel token0 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp0.Content.ReadAsStringAsync().Result);
                Assert.Equal(1, token0.DeviceId);

                // Update account with the created token.
                request = new UserAccountInputModel
                {
                    DisplayName = "Account Display Name 🥃",
                };

                req = new HttpRequestMessage(new HttpMethod("PATCH"), $"http://localhost:8080/api/accounts/users/{accountName}");
                req.Content = new StringContent(JsonConvert.SerializeObject(request));
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                (new ChatAppAuthProvider($"UA:{accountName}", token0.DeviceId, token0.Token)).AuthIt(req.Content.Headers);
                resp = client.SendAsync(req).Result;
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

                HttpRequestMessage reqMsg = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:8080/api/accounts/users/{accountName}");
                ChatAppAuthProvider.Instace.AuthIt(reqMsg.Headers); // Auth request
                resp = client.SendAsync(reqMsg).Result;
                var response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);
            }
        }

        private string CreateUserAccountAndVerify()
        {
            var epoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string accountName = $"test-account-{(epoch.TotalSeconds % 10000):F4}";
            using (HttpClient client = new HttpClient())
            {
                // Create the account and parse response.
                UserAccountInputModel request = new UserAccountInputModel
                {
                    AccountName = accountName,
                    DisplayName = "Account Display Name 🐵",
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                HttpResponseMessage resp = client.PostAsync("http://localhost:8080/api/accounts/users", content).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
                UserAccountViewModel response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);

                // Retreive the account back.
                HttpRequestMessage reqMsg = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:8080/api/accounts/users/{response.Id}");
                ChatAppAuthProvider.Instace.AuthIt(reqMsg.Headers); // Auth request
                resp = client.SendAsync(reqMsg).Result;

                response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);
            }

            return accountName;
        }
    }
}
