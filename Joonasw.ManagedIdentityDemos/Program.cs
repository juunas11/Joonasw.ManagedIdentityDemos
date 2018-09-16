using Joonasw.ManagedIdentityDemos.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Joonasw.ManagedIdentityDemos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseAzureKeyVaultConfiguration()
                .UseStartup<Startup>();
    }
}
