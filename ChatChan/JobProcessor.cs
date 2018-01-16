namespace ChatChan
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using ChatChan.BackendJob;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class JobProcessor
    {
        private readonly JobHost jobHost;

        public JobProcessor()
        {
            // Load configurations.
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configuration = builder.Build();

            // Dependency init.
            ServiceCollection services = new ServiceCollection();
            JobProcessor.AddDependencies(services, configuration);

            // Create logger
            this.jobHost = (services.BuildServiceProvider()).GetRequiredService<JobHost>();
        }

        private static void AddDependencies(IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterAppDependencies(configuration);
            services.AddLogging(lb =>
            {
                IConfigurationSection loggingSection = configuration.GetSection("Logging");
                lb.AddConfiguration(loggingSection);
                lb.AddConsole();
                lb.AddDebug();
            });

            services.AddTransient<SendChatMessageProcessor>();

            services.AddSingleton<JobHost>();
        }

        public Task DoWork()
        {
            return this.jobHost.Loop();
        }
    }
}
