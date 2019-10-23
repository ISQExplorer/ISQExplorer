using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ISQExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var professorCsv = Environment.GetEnvironmentVariable("ISQEXPLORER_IMPORT");
            if (professorCsv != null)
            {
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;
                ProfessorSeeder.Initialize(services, professorCsv);
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}