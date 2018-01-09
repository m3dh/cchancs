namespace ChatChan.Tests.Functional
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Xunit;

    public class ImageServiceTests : IDisposable
    {
        private readonly IWebHost host;

        public ImageServiceTests()
        {
            this.host = Program.GetWebHost(new string[0]);
            this.host.Start();
        }

        public void Dispose()
        {
            this.host?.Dispose();
        }

        [Fact]
        public void CreateAndDownloadAvatarImages_ShouldMatch()
        {
        }
    }
}
