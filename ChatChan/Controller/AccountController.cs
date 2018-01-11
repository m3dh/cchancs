namespace ChatChan.Controller
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Common;
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

    public class UserAccountInputModel
    {
        [JsonProperty(PropertyName = "account_name")]
        public string AccountName { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "avatar_image_id")]
        public string AvatarImageId { get; set; }

        [JsonProperty(PropertyName = "password")]
        public Uri Password { get; set; }
    }

    public class AccountController : Controller
    {
        private readonly IAccountService accountService;

        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
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

        [HttpPatch, Route("api/accounts/users/{accountId}")]
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

            UserAccount account = await this.accountService.UpdateUserAccount(accountIdObj, inputAccount.DisplayName, inputAccount.AvatarImageId);
            return UserAccountViewModel.FromStoreModel(account);
        }
    }
}
