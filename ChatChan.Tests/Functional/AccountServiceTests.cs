namespace ChatChan.Tests.Functional
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
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
        public void UpdateAccount_ShouldBePersisted()
        {
            string accountName = this.CreateUserAccountAndVerify();
            using (HttpClient client = new HttpClient())
            {
                UserAccountInputModel request = new UserAccountInputModel
                {
                    DisplayName = "Account Display Name 🥃",
                };

                HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("PATCH"), $"http://localhost:8080/api/accounts/users/{accountName}");
                req.Content = new StringContent(JsonConvert.SerializeObject(request));
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                req.Content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp = client.SendAsync(req).Result;
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

                resp = client.GetAsync($"http://localhost:8080/api/accounts/users/{accountName}").Result;
                var response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);
            }
        }

        private string CreateUserAccountAndVerify()
        {
            string accountName = $"test-account-{(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds}";
            using (HttpClient client = new HttpClient())
            {
                // Create the account and parse response.
                UserAccountInputModel request = new UserAccountInputModel
                {
                    AccountName = accountName,
                    DisplayName = "Account Display Name 🐵",
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentType.CharSet = "utf-8";
                HttpResponseMessage resp = client.PostAsync("http://localhost:8080/api/accounts/users", content).Result;
                Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
                UserAccountViewModel response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);

                // Retreive the account back.
                resp = client.GetAsync($"http://localhost:8080/api/accounts/users/{response.Id}").Result;
                response = JsonConvert.DeserializeObject<UserAccountViewModel>(resp.Content.ReadAsStringAsync().Result);
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal($"UA:{accountName}", response.Id);
                Assert.Equal(request.DisplayName, response.DisplayName);
            }

            return accountName;
        }
    }
}
