namespace ChatChan
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost webHost = WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://*:8080").UseStartup<Startup>()
                .Build();

            webHost.Run();
        }
    }
}
