using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
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

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            
            var professorJson = Environment.GetEnvironmentVariable("ISQEXPLORER_PROFESSORS_IMPORT");
            if (professorJson != null)
            {
                DatabaseImporter.ImportProfessors(services, professorJson);
            }

            var classJson = Environment.GetEnvironmentVariable("ISQEXPLORER_CLASSES_IMPORT");
            if (classJson != null)
            {
                DatabaseImporter.ImportClasses(services, classJson);
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}