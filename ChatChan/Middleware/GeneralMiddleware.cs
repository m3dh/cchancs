namespace ChatChan.Middleware
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class ErrorResponse
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TrackId { get; set; }
    }

    public class GeneralMiddleware
    {
        private readonly ILogger<GeneralMiddleware> logger;

        public GeneralMiddleware(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<GeneralMiddleware>();
        }

        // This middleware is the first one in the customized pipeline, we can only modify headers in this one.
        public IApplicationBuilder Hook(IApplicationBuilder app)
        {
            return app.Use(this.ExceptionConverter).Use(this.ResponseModifier);
        }

        private async Task ExceptionConverter(HttpContext context, Func<Task> next)
        {
            string trackId = context.TraceIdentifier;

            try
            {
                // TODO [P1] : Set thread context with trackId.
                await next();
            }
            catch (Exception ex)
            {
                this.logger.LogDebug("Exception caught when doing next() : {0}", ex.GetType().Name);

                context.Response.Clear();
                ErrorResponse response = new ErrorResponse { TrackId = trackId };
                if (ex is ClientInputException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.ErrorCode = context.Response.StatusCode;
                    response.ErrorMessage = ex.Message;
                }
                else
                {
                    context.Response.StatusCode = 500;
                    response.ErrorCode = context.Response.StatusCode;
                    response.ErrorMessage = "Internal server error";
                }

                using (StreamWriter writer = new StreamWriter(context.Response.Body, Encoding.UTF8))
                {
                    try
                    {
                        await writer.WriteAsync(JsonConvert.SerializeObject(response));
                    }
                    catch (Exception ex1)
                    {
                        this.logger.LogWarning("Unable to finish error response : {0}", ex1);
                        throw;
                    }
                }
            }
        }

        private async Task ResponseModifier(HttpContext reqContext, Func<Task> next)
        {
            DateTime startDateTime = DateTime.Now; // 1. Performance tracker

            reqContext.Response.OnStarting(state => {
                HttpContext respContext = (HttpContext)state;

                TimeSpan internalElapsed = DateTime.Now - startDateTime;

                // 1. Performance tracker (2)
                respContext.Response.Headers.Add("X-Response-Time",
                    new[] { internalElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture) });

                // 2. Status code converted.
                if (string.Equals(reqContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) && respContext.Response.StatusCode == 200)
                {
                    respContext.Response.StatusCode = 201;
                }

                return Task.FromResult(0);
            }, reqContext);

            await next();
            this.logger.LogInformation($"Request to {reqContext.Request.Path} finished in {(DateTime.Now - startDateTime).TotalMilliseconds}ms");
        }
    }
}
