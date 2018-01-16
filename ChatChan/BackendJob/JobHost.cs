namespace ChatChan.BackendJob
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    public interface IJobProcessor<in TMsg>
    {
        Task Process(TMsg message);
    }

    public class JobHost
    {
        private readonly ILogger logger;

        public JobHost(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<JobHost>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Loop()
        {
            this.logger.LogInformation("Job host started");
            while (true)
            {
                await Task.Delay(5000);
            }
        }
    }
}
