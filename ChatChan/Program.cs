namespace ChatChan
{
    using System.Threading.Tasks;
    using ChatChan.BackendJob;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (IWebHost webHost = Program.GetWebHost(args))
            {
                // Backend jobs shall be initialized prior to starting the service.
                await JobHost.Instance.Initialize();

                // Start the Kestrel server for the chan service.
                await webHost.StartAsync();

                // Host the backend processor thread.
                await JobHost.Instance.Run();
            }
        }

        public static IWebHost GetWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://*:8080")
                .UseStartup<Startup>()
                .Build();
        }
    }
}
