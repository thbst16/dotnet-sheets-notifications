using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace dotnet_sheets_notifications
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((hostingContext, config) => {
                    IHostEnvironment env = hostingContext.HostingEnvironment;
                    var parentDir = Directory.GetParent(hostingContext.HostingEnvironment.ContentRootPath);
                    var appSeetingsPath = string.Concat(env.ContentRootPath, "/config/appsettings.json");
                    config.AddJsonFile(appSeetingsPath, optional: false, reloadOnChange: true);
                    var clientSecretsPath = string.Concat(env.ContentRootPath, "/config/client_secrets-sample.json");
                    config.AddJsonFile(clientSecretsPath, optional: false, reloadOnChange: true);
                    IConfigurationRoot configurationRoot = config.Build();
                });
    }
}
