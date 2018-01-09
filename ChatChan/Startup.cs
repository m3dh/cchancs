namespace ChatChan
{
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

        public Startup(IConfiguration configuration)
        {
            // Get configuration sections for the service usage (ConfigureServices).
            this.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                // Configurations (appsettings.json)
                .Configure<StorageSection>(this.Configuration.GetSection("Storage"))

                // Providers
                .AddSingleton<IImageService, ImageService>()

                // Framework
                .AddMvc()
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            GeneralMiddleware middleware = new GeneralMiddleware(loggerFactory);
            middleware.Hook(app).UseMvc();
        }
    }
}
