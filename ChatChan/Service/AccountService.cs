namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface IAccountService
    {
        Task<int> CountUserAccount();

        Task<AccountId> CreateUserAccount(string accountName, string displayName);
        Task<UserAccount> GetUserAccount(AccountId accountId);
        Task<UserAccount> UpdateUserAccount(AccountId accountId, string password);
        Task<UserAccount> UpdateUserAccount(AccountId accountId, string displayName, string avatarImageId);

        Task<UserClientToken> LogonUserAccount(AccountId accountId, string password, int? deviceId);
    }

    public class AccountService : IAccountService
    {
        private readonly ILogger<ImageService> logger;
        private readonly CoreDbProvider coreDb;
        private readonly ITokenService tokenService;
        private readonly LimitationsSection serviceLimits;

        [Flags]
        private enum UserAccountStatus : long
        {
            CanUpdateFields = 0x00000001,
            CanUpdatePassword = 0x00000002,
            IsNew = 0x00000004,
            LogonBlocked = 0x00000008,

            NewAccount = IsNew | CanUpdatePassword | CanUpdateFields,
        }

        public AccountService(
            ILoggerFactory loggerFactory,
            CoreDbProvider coreDb,
            ITokenService tokenService,
            IOptions<LimitationsSection> serviceLimits)
        {
            this.logger = loggerFactory.CreateLogger<ImageService>();
            this.coreDb = coreDb ?? throw new ArgumentNullException(nameof(coreDb));
            this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            this.serviceLimits = serviceLimits?.Value ?? throw new ArgumentNullException(nameof(serviceLimits));
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
                    { "@partition", 0 }, // TODO : should fetch partitioner.
                    { "@status", (long)UserAccountStatus.NewAccount },
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

        public async Task<UserAccount> UpdateUserAccount(AccountId accountId, string password)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            UserAccount account = await this.GetUserAccount(accountId);
            if (((UserAccountStatus)account.Status & UserAccountStatus.CanUpdatePassword) == 0)
            {
                throw new NotAllowed("Updating an account that is blocked updating password is not allowed.");
            }

            if ((DateTimeOffset.UtcNow - account.UpdatedAt).TotalSeconds > this.serviceLimits.AllowedSetAccountPaswordIntervalSecs)
            {
                throw new NotAllowed("Account password setting exceeded the max allowed time window.");
            }

            // Calc the new password hash.
            string passwordHash = this.GetAccountPassword(password, account.Id);

            // Clear the two update password related bit masks.
            long newAccountStatus = (long)(((UserAccountStatus)account.Status & ~UserAccountStatus.CanUpdatePassword) & ~UserAccountStatus.IsNew);

            (int affected, long lastId) = await this.coreDb.Execute(AccountQueries.UserAccountUpdatePassword, new Dictionary<string, object>
            {
                { "@passwd", passwordHash },
                { "@new_status", newAccountStatus },
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

        public async Task<UserAccount> UpdateUserAccount(AccountId accountId, string displayName, string avatarImageId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            UserAccount account = await this.GetUserAccount(accountId);
            if (((UserAccountStatus)account.Status & UserAccountStatus.CanUpdateFields) == 0)
            {
                throw new NotAllowed("Updating a locked account is not allowed.");
            }

            bool needUpdate = false;
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

        public async Task<UserClientToken> LogonUserAccount(AccountId accountId, string password, int? deviceId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            UserAccount account = await this.GetUserAccount(accountId);
            if (((UserAccountStatus)account.Status & UserAccountStatus.CanUpdatePassword) != 0)
            {
                throw new NotAllowed("Logon to a account being updated password is not allowed.");
            }

            if (((UserAccountStatus)account.Status & UserAccountStatus.LogonBlocked) != 0)
            {
                throw new NotAllowed("The account is blocked for logons.");
            }

            string passwordHash = this.GetAccountPassword(password, account.Id);
            if (!string.Equals(passwordHash, account.Password, StringComparison.OrdinalIgnoreCase))
            {
                throw new Unauthorized($"Incorrect password for account {account.AccountName}");
            }

            return await this.tokenService.GetUserAccountClientToken(accountId, deviceId);
        }

        private string GetAccountPassword(string clientPassword, int accountRecId)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    byte[] accountIdBytes = Encoding.UTF8.GetBytes(accountRecId.ToString());
                    byte[] passwordBytes = Convert.FromBase64String(clientPassword);
                    memory.Write(accountIdBytes, 0, accountIdBytes.Length);
                    memory.Write(passwordBytes, 0, passwordBytes.Length);
                    using (SHA256 hash = new SHA256Managed())
                    {
                        return Convert.ToBase64String(hash.ComputeHash(memory.ToArray()));
                    }
                }
            }
            catch (FormatException ex)
            {
                this.logger.LogInformation("Account password encoding failed with : {0}", ex.Message);
                throw new BadRequest(nameof(clientPassword));
            }
        }
    }
}