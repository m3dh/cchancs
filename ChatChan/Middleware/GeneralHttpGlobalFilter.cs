namespace ChatChan.Middleware
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;

    // A general filter to handle HTTP status code conversions and performance tracking.
    public class GeneralHttpGlobalFilter : IAsyncActionFilter, IExceptionFilter
    {
        private readonly ILogger logger;

        public GeneralHttpGlobalFilter(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<GeneralHttpGlobalFilter>();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            DateTime startDateTime = DateTime.Now; // 1. Performance tracker : Start

            await next();

            TimeSpan elapsed = DateTime.Now - startDateTime;

            // 1. Performance tracker : Write header
            context.HttpContext.Response.Headers.Add("X-Response-Time",
                new[] { elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) });

            // 2. Status code converted.
            if (string.Equals(context.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
                && context.HttpContext.Response.StatusCode == 200)
            {
                context.HttpContext.Response.StatusCode = 201;
            }

            this.logger.LogInformation(
                "Request to {0} finished in {1}ms",
                context.HttpContext.Request.Path,
                elapsed.TotalMilliseconds.ToString("F1"));
        }

        public void OnException(ExceptionContext context)
        {
            this.logger.LogDebug("Exception caught when doing next() : {0}", context.Exception.GetType().Name);

            context.HttpContext.Response.Clear();

            ErrorResponse response = new ErrorResponse { TrackId = context.HttpContext.TraceIdentifier };
            if (context.Exception is ClientInputException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.ErrorCode = context.HttpContext.Response.StatusCode;
                response.ErrorMessage = context.Exception.Message;
            }
            else
            {
                context.HttpContext.Response.StatusCode = 500;
                response.ErrorCode = context.HttpContext.Response.StatusCode;
                response.ErrorMessage = "Internal server error";
            }

            context.Result = new JsonResult(response);
        }
    }
}
