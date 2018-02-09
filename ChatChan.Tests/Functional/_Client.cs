﻿namespace ChatChan.Tests.Functional
{
    using System;
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
        private readonly string accountId;
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
            HttpResponseMessage resp = HttpClient.PostAsync("http://localhost:8080/api/accounts/users", content).Result;
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

            HttpRequestMessage req = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/password");
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

            HttpRequestMessage req0 = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:8080/api/accounts/users/{accountName}/tokens");
            req0.Content = new StringContent(JsonConvert.SerializeObject(request)); // reuse the request
            req0.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req0.Content.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage resp0 = HttpClient.SendAsync(req0).Result;
            Assert.Equal(HttpStatusCode.Created, resp0.StatusCode);
            DeviceTokenViewModel token0 = JsonConvert.DeserializeObject<DeviceTokenViewModel>(resp0.Content.ReadAsStringAsync().Result);
            return token0;
        }

        public ChatClient(string accountName, DeviceTokenViewModel token)
        {
            this.accountId = accountName;
            this.deviceId = token.DeviceId;
            this.token = token.Token;
        }

        private void AuthHeaders(HttpHeaders headers)
        {
            headers.Add(Constants.UserHeaderName, new[] { this.accountId });
            headers.Add(Constants.TokenHeaderName, new[] { $"{this.deviceId}:{this.token}" });
        }
    }
}
