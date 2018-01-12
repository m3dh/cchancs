namespace ChatChan.Middleware
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;

    public class TokenAuthActionFilter : ActionFilterAttribute
    {
        private readonly ITokenService tokenService;
        private readonly ILogger logger;

        public TokenAuthActionFilter(ITokenService tokenService, ILoggerFactory loggerFactory)
        {
            this.tokenService = tokenService;
            this.logger = loggerFactory.CreateLogger<TokenAuthActionFilter>();
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            this.logger.LogDebug("Action {0} is being called in token auth", context.ActionDescriptor.DisplayName);
            if (context.HttpContext.Request.Headers.ContainsKey(Constants.UserHeaderName)
                && context.HttpContext.Request.Headers.ContainsKey(Constants.TokenHeaderName))
            {
                string accountHeader = context.HttpContext.Request.Headers[Constants.UserHeaderName];
                string tokenHeader = context.HttpContext.Request.Headers[Constants.TokenHeaderName];

                if (!AccountId.TryParse(accountHeader, out AccountId accountId))
                {
                    throw new Unauthorized("Account is not provided.");
                }

                (int deviceId, string token) = ParseTokenHeader(tokenHeader);
                if (await this.tokenService.CheckToken(accountId, token, deviceId))
                {
                    context.HttpContext.Items[Constants.HttpContextRealUserNameKey] = accountId.ToString();
                    this.logger.LogDebug("Authentication succeeded for user {0}", accountId.ToString());
                    await base.OnActionExecutionAsync(context, next);
                }
                else
                {
                    throw new Unauthorized("Token specified could not be validated.");
                }
            }
            else
            {
                throw new BadRequest("Auth token and user name shall be provided for this API.", context.HttpContext.Request.Headers);
            }
        }

        private static Tuple<int, string> ParseTokenHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new Unauthorized("Token is not provided.");
            }

            string[] splits = header.Split(':');
            if (splits.Length != 2 || !int.TryParse(splits[0], out int deviceId))
            {
                throw new BadRequest(nameof(header), header);
            }

            return Tuple.Create(deviceId, splits[1]);
        }
    }
}
