namespace ChatChan.Service
{
    using System.Threading.Tasks;
    using ChatChan.Service.Model;

    public interface IChannelService
    {
        Task<Channel> CreateChannel();
    }

    public class ChannelService : IChannelService
    {
    }
}
