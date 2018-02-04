﻿namespace ChatChan.Tests.Functional
{
    using System;
    using System.Net.Http.Headers;

    using ChatChan.Common;
    using ChatChan.Controller;

    public class ChatAppAuthProvider
    {
        private static ChatAppAuthProvider instance;

        private readonly string accountId;
        private readonly string token;
        private readonly int deviceId;

        private ChatAppAuthProvider()
        {
            string accountName = $"auth-account-{(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds}";

            // Create the account and parse response.
            UserAccountViewModel response = ChatClient.CreateUserAccount(accountName, "Test Account for Auth");

            // Now set account password
            response = ChatClient.SetUserAccountPassword(accountName, "CHATCHAN_APP");

            // Logon.
            var token0 = ChatClient.LogonAccount(accountName, "CHATCHAN_APP");

            this.accountId = response.Id;
            this.deviceId = token0.DeviceId;
            this.token = token0.Token;
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
