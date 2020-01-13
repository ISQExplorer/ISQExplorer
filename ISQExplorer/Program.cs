using System;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ISQExplorer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetService<ISQExplorerContext>();

            if (db.IsqEntries.None())
            {
                Print.Line("Scraping data...", ConsoleColor.Green);
                var res = await DataScraper.ScrapeAll();
                if (!res)
                {
                    Print.Line(res.ToString(), ConsoleColor.Yellow);
                    Print.Line("Failed to scrape data.", ConsoleColor.Yellow);
                    return;
                }

                Print.Line("Done scraping data!", ConsoleColor.Green);

                Print.Line("Importing scraped data into database...", ConsoleColor.Green);
                try
                {
                    await Task.WhenAll(
                        db.Courses.AddRangeAsync(res.Value.Courses.Succeeded),
                        db.IsqEntries.AddRangeAsync(res.Value.Entries),
                        db.Professors.AddRangeAsync(res.Value.Professors.Succeeded)
                    );
                }
                catch (Exception e)
                {
                    Print.Line(e.ToString(), ConsoleColor.Yellow);
                    Print.Line("Failed to write scraped data to database.", ConsoleColor.Red);
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}