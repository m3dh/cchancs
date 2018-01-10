﻿namespace ChatChan.Middleware
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

    public class ErrorResponse
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TrackId { get; set; }
    }

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
            // Performance tracker : Start
            DateTime startDateTime = DateTime.Now;

            await next();

            TimeSpan elapsed = DateTime.Now - startDateTime;

            // Performance tracker : Write header
            context.HttpContext.Response.Headers.Add("X-Response-Time",
                new[] { elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) });

            this.logger.LogInformation("Request to {0} finished in {1}ms", context.HttpContext.Request.Path, elapsed.TotalMilliseconds.ToString("F1"));

            // Status code converted.
            if (string.Equals(context.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
                && context.HttpContext.Response.StatusCode == 200)
            {
                context.HttpContext.Response.StatusCode = 201;
            }
        }

        public void OnException(ExceptionContext context)
        {
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

                this.logger.LogDebug("Unhandled exception : {0} : {1}", context.Exception.GetType().Name, context.Exception);
            }

            this.logger.LogDebug("Error : {0}", response.ErrorMessage);
            context.Result = new JsonResult(response);
        }
    }
}
