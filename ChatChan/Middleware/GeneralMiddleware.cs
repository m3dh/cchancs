namespace ChatChan.Middleware
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

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
            return app.Use(this.DoWork);
        }

        private async Task DoWork(HttpContext reqContext, Func<Task> next)
        {
            DateTime startDateTime = DateTime.Now; // 1. Performance tracker

            reqContext.Response.OnStarting(state => {
                HttpContext respContext = (HttpContext)state;

                TimeSpan internalElapsed = DateTime.Now - startDateTime;

                // 1. Performance tracker (2)
                respContext.Response.Headers.Add("X-Response-Time-Milliseconds",
                    new[] { internalElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) });

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
