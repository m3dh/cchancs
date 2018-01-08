namespace ChatChan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ChatChan.Common.Configuration;
    using Newtonsoft.Json;
    using Microsoft.Extensions.Options;

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
            // Configurations (appsettings.json)
            services.Configure<StorageSection>(this.Configuration.GetSection("Storage"));
            
            // Framework
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IOptions<StorageSection> section)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var logger = loggerFactory.CreateLogger<Startup>();
            app.UseMvc();
        }
    }
}
