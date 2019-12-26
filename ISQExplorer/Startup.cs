using System;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ISQExplorer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            if (Environment.GetEnvironmentVariable("ISQEXPLORER_SERVER") == null ||
                Environment.GetEnvironmentVariable("ISQEXPLORER_DB") == null ||
                Environment.GetEnvironmentVariable("ISQEXPLORER_USER") == null ||
                Environment.GetEnvironmentVariable("ISQEXPLORER_PASSWORD") == null)
            {
                throw new ArgumentException("Environment variables ISQEXPLORER_SERVER, ISQEXPLORER_DB, ISQEXPLORER_USER, and ISQEXPLORER_PASSWORD must be set.");
            }

            string connString =
                $"Server={Environment.GetEnvironmentVariable("ISQEXPLORER_SERVER")};Port={Environment.GetEnvironmentVariable("ISQEXPLORER_PORT") ?? "5432"};Database={Environment.GetEnvironmentVariable("ISQEXPLORER_DB")};User Id={Environment.GetEnvironmentVariable("ISQEXPLORER_USER")};Password={Environment.GetEnvironmentVariable("ISQEXPLORER_PASSWORD")};";
            
            services.AddDbContext<ISQExplorerContext>(options => options.UseNpgsql(connString));
            services.AddScoped<IQueryRepository, QueryRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}