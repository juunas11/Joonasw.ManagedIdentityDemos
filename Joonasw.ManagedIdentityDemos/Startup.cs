using Azure.Core;
using Azure.Identity;
using Joonasw.ManagedIdentityDemos.Background;
using Joonasw.ManagedIdentityDemos.Contracts;
using Joonasw.ManagedIdentityDemos.Data;
using Joonasw.ManagedIdentityDemos.Options;
using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace Joonasw.ManagedIdentityDemos
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSignalR();

            services.AddTransient<IDemoService, DemoService>();
            services.AddSingleton<IHostedService, QueueListenerService>();
            services.AddSingleton<IHostedService, EventHubsListenerService>();

            services.Configure<DemoSettings>(Configuration.GetSection("Demo"));

            DemoSettings demoSettings = Configuration.GetSection("Demo").Get<DemoSettings>();
            var managedIdentityInterceptor = new ManagedIdentityConnectionInterceptor(demoSettings);
            services.AddDbContext<MsiDbContext>(o =>
                o.UseSqlServer(demoSettings.SqlConnectionString).AddInterceptors(managedIdentityInterceptor));

            services.AddApplicationInsightsTelemetry(o =>
            {
                o.EnableQuickPulseMetricStream = true;
                o.ConnectionString = Configuration["ApplicationInsights:ConnectionString"];
            });

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                SharedTokenCacheTenantId = demoSettings.ManagedIdentityTenantId,
                VisualStudioCodeTenantId = demoSettings.ManagedIdentityTenantId,
                VisualStudioTenantId = demoSettings.ManagedIdentityTenantId,
            });
            services.AddSingleton<TokenCredential>(credential);
            TokenCredential customApiCredential = string.IsNullOrEmpty(demoSettings.CustomApiClientSecret)
                ? credential
                : new ClientSecretCredential(
                    demoSettings.CustomApiTenantId,
                    demoSettings.CustomApiClientId,
                    demoSettings.CustomApiClientSecret);
            services.AddHttpClient<CustomApiClient, CustomApiClient>(
                (httpClient, serviceProvider) =>
                {
                    var settings = serviceProvider.GetRequiredService<IOptionsSnapshot<DemoSettings>>();
                    return new CustomApiClient(httpClient, settings, customApiCredential);
                });
            services.AddHttpClient<MapsApiClient>();

            services.AddSingleton((IServiceProvider _) =>
            {
                return new CosmosClient(Configuration["Demo:CosmosDbAccountUri"], credential);
            });

            services.AddAzureClients(clients =>
            {
                clients.AddBlobServiceClient(new Uri($"https://{demoSettings.StorageAccountName}.blob.core.windows.net"));
                clients.AddDataLakeServiceClient(new Uri($"https://{demoSettings.DataLakeStoreName}.blob.core.windows.net"));
                clients.AddEventHubProducerClientWithNamespace($"{demoSettings.EventHubNamespace}.servicebus.windows.net", demoSettings.EventHubName);
                clients.AddServiceBusClientWithNamespace($"{demoSettings.ServiceBusNamespace}.servicebus.windows.net");
                clients.AddSecretClient(new Uri(demoSettings.KeyVaultBaseUrl));
                clients.AddTextAnalyticsClient(new Uri(demoSettings.CognitiveServicesBaseUrl));
                clients.AddConfigurationClient(new Uri(demoSettings.AppConfigUrl));
                clients.UseCredential(credential);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(o =>
            {
                o.MapHub<QueueMessageHub>("/queueMessages");
                o.MapHub<EventHubMessageHub>("/eventHubMessages");
                o.MapControllerRoute("default", "{controller=Demo}/{action=Index}/{id?}");
            });
        }
    }
}
