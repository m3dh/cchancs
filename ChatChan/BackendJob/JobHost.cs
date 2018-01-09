namespace ChatChan.BackendJob
{
    using System.Threading.Tasks;

    public class JobHost
    {
        private JobHost() { }

        public static JobHost Instance { get; private set; }

        public static void CreateSingleInstance()
        {
            JobHost.Instance = new JobHost();
        }

        public Task Initialize()
        {
            return Task.FromResult(0);
        }

        public async Task Run()
        {
            while (true)
            {
                await Task.Delay(5000);
            }
        }
    }
}
