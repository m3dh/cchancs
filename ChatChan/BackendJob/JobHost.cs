namespace ChatChan.BackendJob
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    public interface IJobProcessor<in TMsg>
    {
        Task Process(TMsg message);
    }

    public class JobHost
    {
        private readonly IMessageQueue queue;
        private readonly IJobProcessor<SendChatMessageEvent> sendChatMessageProcessor;
        private readonly ILogger logger;

        public JobHost(
            ILoggerFactory loggerFactory,
            IMessageQueue queue,
            IJobProcessor<SendChatMessageEvent> sendChatMessageProcessor)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.sendChatMessageProcessor = sendChatMessageProcessor ?? throw new ArgumentNullException(nameof(sendChatMessageProcessor));
            this.logger = loggerFactory.CreateLogger<JobHost>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Loop()
        {
            this.logger.LogInformation("Job host started");
            while (true)
            {
                IQueueEvent queueEvent = await this.queue.Pop();
                if (queueEvent != null)
                {
                    switch (queueEvent.DataType)
                    {
                        case ChatAppQueueEventTypes.SendMessage:
                            SendChatMessageEvent sendChatMsg = JsonConvert.DeserializeObject<SendChatMessageEvent>(queueEvent.DataJson);
                            await this.sendChatMessageProcessor.Process(sendChatMsg);
                            break;

                        default:
                            this.logger.LogError($"Unexpected event type {queueEvent.DataType}, ignoring...");
                            break;
                    }
                }
                else
                {
                    this.logger.LogInformation("No new queue event found, sleeping...");
                    await Task.Delay(5000);
                }
            }
        }
    }
}
