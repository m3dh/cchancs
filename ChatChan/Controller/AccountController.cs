namespace ChatChan.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Middleware;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.AspNetCore.Mvc;

    using Newtonsoft.Json;

    public class UserAccountViewModel
    {
        [JsonProperty(PropertyName = "account_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "avatar_image_id")]
        public string AvatarImageId { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty(PropertyName = "avatar_url")]
        public Uri AvatarUri { get; set; }

        public static UserAccountViewModel FromStoreModel(UserAccount account)
        {
            return new UserAccountViewModel
            {
                Id = account.AccountId.ToString(),
                DisplayName = account.DisplayName,
                AvatarImageId = account.Avatar?.ToString(),
                AvatarUri = account.Avatar == null ? null : ImageController.GetImageUri(account.Avatar),
                CreatedAt = account.CreatedAt,
            };
        }
    }

    public class DeviceTokenViewModel
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("device_id")]
        public int DeviceId { get; set; }

        [JsonProperty("expire")]
        public DateTimeOffset ExpireAt { get; set; }
    }

    public class UserAccountInputModel
    {
        [JsonProperty(PropertyName = "account_name")]
        public string AccountName { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "avatar_image_id")]
        public string AvatarImageId { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
    }

    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;

        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        [HttpGet, Route("api/accounts/count")]
        public async Task<int> CountAllAccounts()
        {
            return await this.accountService.CountUserAccount();
        }

        [HttpPost, Route("api/accounts/users")]
        public async Task<UserAccountViewModel> CreateUserAccount([FromBody] UserAccountInputModel inputAccount)
        {
            if (inputAccount == null)
            {
                throw new BadRequest(nameof(inputAccount));
            }

            if (string.IsNullOrEmpty(inputAccount.AccountName))
            {
                throw new BadRequest(nameof(inputAccount.AccountName));
            }

            if (string.IsNullOrEmpty(inputAccount.DisplayName))
            {
                throw new BadRequest(nameof(inputAccount.DisplayName));
            }

            AccountId accountId = await this.accountService.CreateUserAccount(inputAccount.AccountName, inputAccount.DisplayName);
            UserAccount account = await this.accountService.GetUserAccount(accountId);
            return UserAccountViewModel.FromStoreModel(account);
        }

        [HttpPost, Route("api/accounts/users/{accountId}/password")]
        public async Task<UserAccountViewModel> CreateUserAccountPassword(string accountId, [FromBody] UserAccountInputModel inputAccount)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest(nameof(accountId));
            }

            if (inputAccount == null)
            {
                throw new BadRequest(nameof(inputAccount));
            }

            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Name = accountId, Type = AccountId.AccountType.UA };
            }

            UserAccount account = await this.accountService.UpdateUserAccount(accountIdObj, inputAccount.Password);
            return UserAccountViewModel.FromStoreModel(account);
        }

        [HttpPost, Route("api/accounts/users/{accountId}/tokens")]
        public async Task<DeviceTokenViewModel> CreateUserAccountDeviceToken(
            string accountId,
            [FromQuery(Name = "device_id")] int? deviceId,
            [FromBody] UserAccountInputModel inputAccount)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest(nameof(accountId));
            }

            if (inputAccount == null)
            {
                throw new BadRequest(nameof(inputAccount));
            }

            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Name = accountId, Type = AccountId.AccountType.UA };
            }

            UserClientToken token = await this.accountService.LogonUserAccount(accountIdObj, inputAccount.Password, deviceId);
            return new DeviceTokenViewModel
            {
                DeviceId = token.DeviceId,
                ExpireAt = token.ExpiredAt,
                Token = token.Token,
            };
        }

        [HttpGet, Route("api/accounts/users/{accountId}")]
        public async Task<UserAccountViewModel> GetUserAccount(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest(nameof(accountId));
            }

            if  (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Name = accountId, Type = AccountId.AccountType.UA };
            }

            UserAccount account = await this.accountService.GetUserAccount(accountIdObj);
            return UserAccountViewModel.FromStoreModel(account);
        }

        [HttpGet, Route("api/accounts/users")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<UserAccountViewModel[]> SearchUserAccount([FromQuery] string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new BadRequest(nameof(prefix));
            }

            IList<UserAccount> accounts = await this.accountService.SearchUserAccount(prefix) ?? new List<UserAccount>();
            return accounts.Select(UserAccountViewModel.FromStoreModel).ToArray();
        }

        [HttpPatch, Route("api/accounts/users/{accountId}")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<UserAccountViewModel> UpdateUserAccount(string accountId, [FromBody] UserAccountInputModel inputAccount)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest(nameof(accountId));
            }

            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Name = accountId, Type = AccountId.AccountType.UA };
            }

            if (inputAccount == null)
            {
                throw new BadRequest(nameof(inputAccount));
            }

            if (!this.IsAuthAccount(accountIdObj))
            {
                throw new Forbidden("Updating another account is not allowed.");
            }

            UserAccount account = await this.accountService.UpdateUserAccount(accountIdObj, inputAccount.DisplayName, inputAccount.AvatarImageId);
            return UserAccountViewModel.FromStoreModel(account);
        }
    }
}
