using Joonasw.ManagedIdentityDemos.Background;
using Joonasw.ManagedIdentityDemos.Contracts;
using Joonasw.ManagedIdentityDemos.Data;
using Joonasw.ManagedIdentityDemos.Options;
using Joonasw.ManagedIdentityDemos.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddControllersWithViews().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
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
                o.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            });
            services.AddHttpClient(HttpClients.CustomApi);
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
