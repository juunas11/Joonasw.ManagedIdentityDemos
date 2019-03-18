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

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR();

            services.AddTransient<IDemoService, DemoService>();
            services.AddSingleton<IHostedService, QueueListenerService>();

            services.Configure<DemoSettings>(Configuration.GetSection("Demo"));

            services.AddDbContext<MsiDbContext>(o => o.UseSqlServer(Configuration["Demo:SqlConnectionString"]));

            services.AddApplicationInsightsTelemetry(o =>
            {
                o.EnableQuickPulseMetricStream = true;
                o.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            });
            services.AddHttpClient(HttpClients.CustomApi);
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
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

            app.UseSignalR(routes =>
            {
                routes.MapHub<QueueMessageHub>("/messages");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Demo}/{action=Index}/{id?}");
            });
        }
    }
}
