namespace ChatChan
{
    using ChatChan.BackendJob;
    using ChatChan.Common.Configuration;
    using ChatChan.Middleware;
    using ChatChan.Provider;
    using ChatChan.Service;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Newtonsoft.Json;

    public class Startup
    {
        private IConfiguration Configuration { get; }
        private IServiceCollection serviceCollection;

        public Startup(IConfiguration configuration)
        {
            // Get configuration sections for the service usage (ConfigureServices).
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            this.serviceCollection = services;
            services
                // Configurations (appsettings.json)
                .Configure<StorageSection>(this.Configuration.GetSection("Storage"))
                .Configure<LimitationsSection>(this.Configuration.GetSection("Limitations"))

                // Providers
                .AddSingleton<CoreDbProvider>()

                // Filters
                .AddTransient<TokenAuthActionFilter>()

                // Entity services
                .AddSingleton<IImageService, ImageService>()
                .AddSingleton<IAccountService, AccountService>()
                .AddSingleton<ITokenService, TokenService>()

                // Framework
                .AddMvc(options => { options.Filters.Add<GeneralHttpGlobalFilter>(); })
                .AddJsonOptions(options => { options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IOptions<StorageSection> storageSection)
        {
            ILogger<Startup> logger = loggerFactory.CreateLogger<Startup>();
            if (env.IsDevelopment())
            {
                logger.LogInformation("Running host process for development");
            }
            else if (env.IsProduction())
            {
                logger.LogInformation("Running host process for production");
            }

            logger.LogInformation("Storage mode : {0}", storageSection.Value?.DeployMode ?? "<empty>");

            // Configure backend job processor.
            JobHost.CreateSingleInstance();

            // Configure request pipeline.
            app.UseMvc();
        }
    }
}