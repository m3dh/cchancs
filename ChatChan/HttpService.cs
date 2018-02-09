namespace ChatChan
{
    using ChatChan.Common.Configuration;
    using ChatChan.Middleware;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Newtonsoft.Json;

    public class HttpService
    {
        private IConfiguration Configuration { get; }

        public HttpService(IConfiguration configuration)
        {
            // Get configuration sections for the service usage (ConfigureServices).
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                // Register key dependencies.
                .RegisterAppDependencies(this.Configuration)

                // Filters
                .AddSingleton<TokenAuthActionFilter>()

                // Framework
                .AddMvcCore(options => { options.Filters.Add<GeneralHttpGlobalFilter>(); })
                .AddFormatterMappings()
                .AddJsonFormatters()
                .AddJsonOptions(options => { options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IOptions<StorageSection> storageSection)
        {
            ILogger<HttpService> logger = loggerFactory.CreateLogger<HttpService>();
            logger.LogInformation("Environment : {0}, Storage mode : {1}", env.EnvironmentName, storageSection.Value?.DeployMode ?? "<empty>");

            // Configure request pipeline.
            app.UseMvc();
        }
    }
}