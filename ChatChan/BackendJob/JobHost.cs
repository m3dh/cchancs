namespace ChatChan.BackendJob
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    public interface IJobProcessor
    {
        Task<bool> ProcessOne();
    }

    public class JobHost
    {
        private readonly IList<IJobProcessor> processors;
        private readonly ILogger logger;

        public JobHost(ILoggerFactory loggerFactory, IList<IJobProcessor> processors)
        {
            this.processors = processors ?? throw new ArgumentNullException(nameof(processors));
            this.logger = loggerFactory.CreateLogger<JobHost>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Loop()
        {
            while (true)
            {
                this.logger.LogWarning("Starts");
                await Task.Delay(5000);
            }
        }
    }
}
