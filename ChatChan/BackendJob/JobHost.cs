namespace ChatChan.BackendJob
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Provider;
    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    public interface IJobProcessor<in TMsg>
    {
        Task<bool> Process(TMsg message);
    }

    public class JobHost
    {
        private readonly MessageQueueProvider queue;
        private readonly IJobProcessor<SendChatMessageEvent> sendChatMessageProcessor;
        private readonly ILogger logger;

        public JobHost(
            ILoggerFactory loggerFactory,
            MessageQueueProvider queue,
            IJobProcessor<SendChatMessageEvent> sendChatMessageProcessor)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.sendChatMessageProcessor = sendChatMessageProcessor ?? throw new ArgumentNullException(nameof(sendChatMessageProcessor));
            this.logger = loggerFactory.CreateLogger<JobHost>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Loop()
        {
            string threadSignature = Guid.NewGuid().ToString("N");
            this.logger.LogInformation($"Job host started (SIG: {threadSignature})");
            while (true)
            {
                IQueueEvent queueEvent = await this.queue.Pop();
                if (queueEvent != null)
                {
                    bool result = false;
                    switch (queueEvent.DataType)
                    {
                        case ChatAppQueueEventTypes.SendMessage:
                            SendChatMessageEvent sendChatMsg = JsonConvert.DeserializeObject<SendChatMessageEvent>(queueEvent.DataJson);
                            result = await this.sendChatMessageProcessor.Process(sendChatMsg);
                            break;

                        default:
                            this.logger.LogError($"Unexpected event type {queueEvent.DataType}, ignoring...");
                            result = true;
                            break;
                    }

                    // Dequeue the event.
                    if (result)
                    {
                        await this.queue.Dequeue(queueEvent);
                    }
                }
                else
                {
                    Task waitTask = Task.Delay(5000);
                    Task awaitTask = await Task.WhenAny(waitTask, this.queue.GetLocalReadiness(threadSignature));
                    if (awaitTask.Id != waitTask.Id)
                    {
                        this.logger.LogDebug($"Job host is awaken (SIG: {threadSignature})");
                    }
                }
            }
        }
    }
}
