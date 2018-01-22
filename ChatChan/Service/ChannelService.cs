namespace ChatChan.Service
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Service.Model;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface IChannelService
    {
        Task<Channel> CreateChannel();
    }

    public class ChannelService : IChannelService
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IDataPartitionProvider partitionProvider;

        public ChannelService(
            ILoggerFactory loggerFactory,
            IDataPartitionProvider partitionProvider)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.partitionProvider = partitionProvider ?? throw new ArgumentNullException(nameof(partitionProvider));
        }

        public Task<Channel> CreateChannel()
        {
            throw new NotImplementedException();
        }
    }
}
