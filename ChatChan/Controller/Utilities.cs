namespace ChatChan.Controller
{
    using System;
    using ChatChan.Common;
    using ChatChan.Service.Identifier;
    using Microsoft.AspNetCore.Mvc;

    public static class Utilities
    {
        public static bool IsAuthAccount(this ControllerBase controller, AccountId accountId)
        {
            if (accountId == null)
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            return string.Equals(
                accountId.ToString(),
                controller.HttpContext.Items[Constants.HttpContextRealUserNameKey].ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        public static AccountId GetAuthAccount(this ControllerBase controller)
        {
            string authAccountStr = controller.HttpContext.Items[Constants.HttpContextRealUserNameKey].ToString();
            if(!AccountId.TryParse(authAccountStr, out AccountId authAccountId))
            {
                throw new Unauthorized("Account authorized failed.");
            }

            return authAccountId;
        }
    }
}
