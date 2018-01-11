namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    public interface IAccountService
    {
        Task<int> CountUserAccount();

        Task<AccountId> CreateUserAccount(string accountName, string displayName);
        Task<UserAccount> GetUserAccount(AccountId accountId);
        Task<UserAccount> UpdateUserAccount(AccountId accountId, string password);
        Task<UserAccount> UpdateUserAccount(AccountId accountId, string displayName, string avatarImageId);

        Task<UserClientToken> LogonUserAccount(AccountId accountId, string password);
    }

    public class AccountService : IAccountService
    {
        private readonly ILogger<ImageService> logger;
        private readonly CoreDbProvider coreDb;

        [Flags]
        private enum UserAccountStatus : long
        {
            CanUpdateFields = 0x00000001,
            CanUpdatePassword = 0x00000002,
            IsNew = 0x00000004,

            NewAccount = IsNew | CanUpdatePassword | CanUpdateFields,
        }

        public AccountService(ILoggerFactory loggerFactory, CoreDbProvider coreDb)
        {
            this.logger = loggerFactory.CreateLogger<ImageService>();
            this.coreDb = coreDb;
        }

        public Task<int> CountUserAccount()
        {
            return this.coreDb.QueryScalar<int>(AccountQueries.UserAccountQueryCount);
        }

        public async Task<AccountId> CreateUserAccount(string accountName, string displayName)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                throw new ArgumentNullException(nameof(accountName));
            }

            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            (int affect, long id) = await this.coreDb.Execute(
                AccountQueries.UserAccountCreation, new Dictionary<string, object>
                {
                    { "@name", accountName },
                    { "@display_name", displayName },
                    { "@status", UserAccountStatus.NewAccount },
                });

            this.logger.LogDebug($"New user account created with 'affect' = {affect}, 'id' = {id}");
            return new AccountId { Name = accountName, Type = AccountId.AccountType.UA };
        }

        public async Task<UserAccount> GetUserAccount(AccountId accountId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            UserAccount account =
                (await this.coreDb.QueryAll<UserAccount>(AccountQueries.UserAccountQueryByAccountName,
                    new Dictionary<string, object> { { "@name", accountId.Name } }))
                .FirstOrDefault();

            if (account == null)
            {
                throw new NotFound($"User account with name {accountId.Name} is not found.");
            }

            return account;
        }

        public async Task<UserAccount> GetUserAccount(int id)
        {
            UserAccount account =
                (await this.coreDb.QueryAll<UserAccount>(AccountQueries.UserAccountQueryById,
                    new Dictionary<string, object> { { "@id", id } }))
                .FirstOrDefault();

            if (account == null)
            {
                throw new NotFound($"User account with ID {id} is not found.");
            }

            return account;
        }

        public Task<UserAccount> UpdateUserAccount(AccountId accountId, string password)
        {
            throw new NotImplementedException();
        }

        public async Task<UserAccount> UpdateUserAccount(AccountId accountId, string displayName, string avatarImageId)
        {
            bool needUpdate = false;
            UserAccount account = await this.GetUserAccount(accountId);
            if (((UserAccountStatus)account.Status & UserAccountStatus.CanUpdateFields) == 0)
            {
                throw new NotAllowed("Updating a locked account is not allowed.");
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                needUpdate = true;
                account.DisplayName = displayName;
            }

            if (!string.IsNullOrEmpty(avatarImageId) && ImageId.TryParse(avatarImageId, out ImageId imageId))
            {
                needUpdate = true;
                account.Avatar = imageId;
            }

            if (needUpdate)
            {
                (int affected, long lastId) = await this.coreDb.Execute(AccountQueries.UserAccountUpdateElseById, new Dictionary<string, object>
                {
                    { "@avatar", account.Avatar?.ToString() },
                    { "@display_name", account.DisplayName },
                    { "@id", account.Id },
                    { "@version", account.Version }
                });

                if (affected == 0)
                {
                    throw new Conflict(Conflict.Code.RaceCondition, "Account is changed by someone else");
                }
                else
                {
                    this.logger.LogDebug("Account updated with affected = '{0}', last ID = '{1}'", affected, lastId);
                    return account;
                }
            }
            else
            {
                throw new NotModified("Account change is not needed");
            }
        }

        public Task<UserClientToken> LogonUserAccount(AccountId accountId, string password)
        {
            throw new NotImplementedException();
        }
    }
}