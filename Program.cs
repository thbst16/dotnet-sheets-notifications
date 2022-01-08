using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System.IO;
using dotnet_sheets_notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((hostingContext, config) => {
    IHostEnvironment env = hostingContext.HostingEnvironment;
    var parentDir = Directory.GetParent(hostingContext.HostingEnvironment.ContentRootPath);
    var appSeetingsPath = string.Concat(env.ContentRootPath, "/config/appsettings.json");
    config.AddJsonFile(appSeetingsPath, optional: false, reloadOnChange: true);
    var clientSecretsPath = string.Concat(env.ContentRootPath, "/config/client_secrets.json");
    config.AddJsonFile(clientSecretsPath, optional: false, reloadOnChange: true);
    IConfigurationRoot configurationRoot = config.Build();
});

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    q.AddJobAndTrigger<SheetsNotificationJob>(builder.Configuration);
});
builder.Services.AddQuartzHostedService(
    q => q.WaitForJobsToComplete = true);

builder.Services.AddHealthChecks()
    .AddProcessAllocatedMemoryHealthCheck(512);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/", new HealthCheckOptions()
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
});
app.Run();