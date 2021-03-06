using System;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;
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
            // initialize services
            var host = CreateHostBuilder(args).Build();

            // get the instance of ISQExplorerContext from asp's dependency injection
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetService<ISQExplorerContext>();

            // if the database is empty
            if (db.IsqEntries.None())
            {
                var scraper = services.GetService<Scraper>();
                
                // scrape the data
                Print.Line("Scraping data...", ConsoleColor.Green);
                var res = await scraper.ScrapeEntriesAsync();
                
                // if scraping failed
                if (!res)
                {
                    // log the error, exit
                    Print.Line(res.ToString(), ConsoleColor.Yellow);
                    Print.Line("Failed to scrape data.", ConsoleColor.Yellow);
                    return;
                }

                Print.Line("Done scraping data!", ConsoleColor.Green);
            }

            // start hosting the app
            host.Run();
        }

        // i don't know what this does. it came with the template
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
