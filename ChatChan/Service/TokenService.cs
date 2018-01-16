namespace ChatChan.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface ITokenService
    {
        Task<UserClientToken> GetUserAccountClientToken(AccountId account, int? deviceId);
        Task<bool> CheckToken(AccountId account, string token, int deviceId);
    }

    public class TokenService : ITokenService
    {
        private readonly CoreDbProvider coreDb;
        private readonly LimitationsSection limits;
        private readonly TimeSpan slidingExpire;
        private readonly TimeSpan absoluteExpire;
        private readonly ILogger logger;

        public TokenService(CoreDbProvider coreDb, IOptions<LimitationsSection> limits, ILoggerFactory loggerFactory)
        {
            this.coreDb = coreDb ?? throw new ArgumentNullException(nameof(coreDb));
            this.limits = limits?.Value ?? throw new ArgumentNullException(nameof(limits));
            this.slidingExpire = TimeSpan.FromHours(this.limits.UserAccountDeviceTokenSlidingExpireInHours);
            this.absoluteExpire = TimeSpan.FromHours(this.limits.UserAccountDeviceTokenAbsoluteExpireInHours);
            this.logger = loggerFactory.CreateLogger<TokenService>();
        }

        public async Task<UserClientToken> GetUserAccountClientToken(AccountId account, int? deviceId)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (account.Type != AccountId.AccountType.UA)
            {
                throw new NotAllowed($"Logon {account.Type} to account via token service is not allowed.");
            }

            IList<UserClientToken> userAccountTokens = await this.ListAllTokensForUserAccount(account);
            UserClientToken deviceToken;
            if (deviceId.HasValue && userAccountTokens.Any(t => t.DeviceId == deviceId.Value))
            {
                // Check if the token for this device is valid, replace it if not.
                deviceToken = userAccountTokens.First(t => t.DeviceId == deviceId.Value);
                if (DateTimeOffset.UtcNow > deviceToken.ExpiredAt || DateTimeOffset.UtcNow > deviceToken.LastGetAt.Add(this.slidingExpire))
                {
                    // Already expired.
                    this.logger.LogDebug("Token {0} expired, refreshing...", deviceToken.Id);
                    deviceToken.Token = Guid.NewGuid().ToString("N");
                    deviceToken.LastGetAt = DateTimeOffset.UtcNow;
                    deviceToken.ExpiredAt = DateTimeOffset.UtcNow.Add(this.absoluteExpire);
                    (int affect, long _) = await this.coreDb.Execute(
                        TokenQueries.TokenRefresh,
                        new Dictionary<string, object>
                        {
                            { "@token", deviceToken.Token },
                            { "@last_get", deviceToken.LastGetAt },
                            { "@expire", deviceToken.ExpiredAt },
                            { "@version", deviceToken.Version },
                            { "@id", deviceToken.Id }
                        });

                    if (affect <= 0)
                    {
                        throw new Conflict(Conflict.Code.RaceCondition, "Someone else is also updating the same token.");
                    }
                }
                else
                {
                    this.logger.LogDebug("Token {0} found, setting the last get time...", deviceToken.Id);
                    deviceToken.LastGetAt = DateTimeOffset.UtcNow;
                    await this.coreDb.Execute(
                        TokenQueries.TokenRefetch,
                        new Dictionary<string, object>
                        {
                            { "@last_get", deviceToken.LastGetAt },
                            { "@id", deviceToken.Id }
                        });
                }
            }
            else
            {
                // Try create a new token, or replace the eldest one.
                if (userAccountTokens.Count >= this.limits.AllowedSingleUserDeviceCount)
                {
                    this.logger.LogDebug("All device token slot used, refreshing the eldest one...");
                    deviceToken = userAccountTokens.OrderBy(t => t.ExpiredAt).First();
                    deviceToken.Token = Guid.NewGuid().ToString("N");
                    deviceToken.LastGetAt = DateTimeOffset.UtcNow;
                    deviceToken.ExpiredAt = DateTimeOffset.UtcNow.Add(this.absoluteExpire);
                    (int affect, long _) = await this.coreDb.Execute(
                        TokenQueries.TokenRefresh,
                        new Dictionary<string, object>
                        {
                            { "@token", deviceToken.Token },
                            { "@last_get", deviceToken.LastGetAt },
                            { "@expire", deviceToken.ExpiredAt },
                            { "@version", deviceToken.Version },
                            { "@id", deviceToken.Id }
                        });

                    if (affect <= 0)
                    {
                        throw new Conflict(Conflict.Code.RaceCondition, "Someone else is also updating the same token.");
                    }
                }
                else
                {
                    deviceToken = new UserClientToken
                    {
                        AccountName = account.Name,
                        DeviceId = userAccountTokens.Count + 1, // Device ID starts from 1.
                        ExpiredAt = DateTimeOffset.UtcNow.Add(this.absoluteExpire),
                        LastGetAt = DateTimeOffset.UtcNow,
                        Token = Guid.NewGuid().ToString("N")
                    };
                    (int affect, long lastId) = await this.coreDb.Execute(
                        TokenQueries.TokenCreateNew,
                        new Dictionary<string, object>
                        {
                            { "@account_name", deviceToken.AccountName },
                            { "@device", deviceToken.DeviceId },
                            { "@token", deviceToken.Token },
                            { "@last_get", deviceToken.LastGetAt },
                            { "@expire", deviceToken.ExpiredAt }
                        }
                    );

                    this.logger.LogInformation("New token created with ID = {0}, affected = {1}", lastId, affect);
                }
            }

            return deviceToken;
        }

        public async Task<bool> CheckToken(AccountId account, string token, int deviceId)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (account.Type != AccountId.AccountType.UA)
            {
                throw new NotAllowed($"Logon {account.Type} to account via token service is not allowed.");
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new BadRequest(nameof(token));
            }

            if (deviceId <= 0)
            {
                throw new BadRequest(nameof(deviceId), deviceId.ToString());
            }

            var deviceToken = (await this.coreDb.QueryAll<UserClientToken>(
                TokenQueries.TokenQueryByAccountNameAndDevice,
                new Dictionary<string, object>
                {
                    { "@account_name", account.Name },
                    { "@device", deviceId }
                })).FirstOrDefault();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (deviceToken == null || now > deviceToken.ExpiredAt || now > deviceToken.LastGetAt.Add(this.slidingExpire))
            {
                throw new Unauthorized($"No valid token could be found to authorize device {deviceId}.");
            }
            else
            {
                await this.coreDb.Execute(
                    TokenQueries.TokenRefetch,
                    new Dictionary<string, object>
                    {
                        { "@last_get", deviceToken.LastGetAt },
                        { "@id", deviceToken.Id }
                    });

                return true;
            }
        }

        private async Task<IList<UserClientToken>> ListAllTokensForUserAccount(AccountId account)
        {
            return await this.coreDb.QueryAll<UserClientToken>(TokenQueries.TokenQueryByAccountName, new Dictionary<string, object>
            {
                { "@account_name", account.Name }
            });
        }
    }
}
