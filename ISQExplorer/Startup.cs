using System;
using ISQExplorer.Database;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ISQExplorer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private static IConnection GetConnection()
        {
            var dbProvider = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_PROVIDER")?.ToLower();
            if (dbProvider == null || dbProvider != "postgres" && dbProvider != "sqlserver")
            {
                Print.Error(
                    "Warning: Environment variable ISQEXPLORER_DB_PROVIDER not set to either 'postgres' or 'sqlserver'. Defaulting to 'sqlserver'",
                    ConsoleColor.Yellow);
                dbProvider = "sqlserver";
            }

            var connString = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_CONNECTION_STRING");
            if (connString != null)
            {
                if (dbProvider == "postgres")
                {
                    return new PostgresConnection(connString);
                }

                return new SqlServerConnection(connString);
            }

            switch (dbProvider)
            {
                case "postgres":
                {
                    var user = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_USER");
                    var password = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_PASSWORD");
                    var host = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_HOST") ?? "localhost";
                    var portString = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_PORT");
                    var db = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_DATABASE") ?? nameof(ISQExplorer);
                    var ssl = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_SSL");
                    var sslSelfSignedString =
                        Environment.GetEnvironmentVariable("ISQEXPLORER_DB_SSL_ALLOW_SELF_SIGNED");

                    var sslType = ssl?.ToLower() switch
                    {
                        var x when
                        x == "required" ||
                        x == "1" ||
                        x == "true" => SslMode.Require,

                        var x when
                        x == "preferred" ||
                        x == null => SslMode.Prefer,

                        var x when
                        x == "disabled" ||
                        x == "0" => SslMode.Disable,

                        var x => throw new ArgumentException(
                            $"Environment variable ISQEXPLORER_DB_SSL for PostgreSQL connections must be set to one of 'required', 'preferred', 'disabled'. Was set to '${x}'")
                    };

                    var sslSelfSigned = sslSelfSignedString?.ToLower() switch
                    {
                        var x when
                        x == "yes" ||
                        x == "true" ||
                        x == "1" => true,

                        var x when
                        x == "0" ||
                        x == "false" ||
                        x == "no" ||
                        x == null => false,

                        var x => throw new ArgumentException(
                            $"Environment variable ISQEXPLORER_DB_SSL_ALLOW_SELF_SIGNED for SQL Server connections must be set to one of '1', '0'. Was set to '${x}'")
                    };

                    var ret = new PostgresConnection
                    {
                        Database = db,
                        Host = host,
                        Password = password,
                        Username = user,
                        SslType = sslType,
                        AllowSelfSigned = sslSelfSigned
                    };

                    if (portString != null)
                    {
                        var port = Parse.Int(portString);
                        if (!port)
                        {
                            throw new ArgumentException(
                                $"Environment variable ISQEXPLORER_DB_PORT must be a number, was {portString})",
                                port.Exception);
                        }

                        ret.Port = port.Value;
                    }

                    return ret;
                }

                case "sqlserver":
                {
                    var user = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_USER");
                    var password = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_PASSWORD");
                    var host = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_HOST");
                    var portString = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_PORT");
                    var db = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_DATABASE") ?? nameof(ISQExplorer);
                    var ssl = Environment.GetEnvironmentVariable("ISQEXPLORER_DB_SSL");
                    var sslSelfSignedString =
                        Environment.GetEnvironmentVariable("ISQEXPLORER_DB_SSL_ALLOW_SELF_SIGNED");
                    var sslTrustedConnectionString =
                        Environment.GetEnvironmentVariable("ISQEXPLORER_DB_SQLSERVER_TRUSTED_CONNECTION");

                    var sslType = ssl?.ToLower() switch
                    {
                        var x when
                        x == "1" ||
                        x == "true" ||
                        x == "yes" => true,

                        var x when
                        x == "0" ||
                        x == "false" ||
                        x == "no" ||
                        x == null => false,

                        var x => throw new ArgumentException(
                            $"Environment variable ISQEXPLORER_DB_SSL for SQL Server connections must be set to one of '1', '0'. Was set to '${x}'")
                    };

                    var sslSelfSigned = sslSelfSignedString?.ToLower() switch
                    {
                        var x when
                        x == "yes" ||
                        x == "true" ||
                        x == "1" => true,

                        var x when
                        x == "0" ||
                        x == "false" ||
                        x == "no" ||
                        x == null => false,

                        var x => throw new ArgumentException(
                            $"Environment variable ISQEXPLORER_DB_SSL_ALLOW_SELF_SIGNED for SQL Server connections must be set to one of '1', '0'. Was set to '${x}'")
                    };

                    var sslTrustedConnection = (user == null && password == null) || sslTrustedConnectionString?.ToLower() switch
                    {
                        var x when
                        x == "yes" ||
                        x == "true" ||
                        x == "1" => true,

                        var x when
                        x == "0" ||
                        x == "false" ||
                        x == "no" ||
                        x == null => false,

                        var x => throw new ArgumentException(
                            $"Environment variable ISQEXPLORER_DB_SQLSERVER_TRUSTED_CONNECTION must be set to one of '1', '0'. Was set to '${x}'")
                    };

                    var ret = new SqlServerConnection
                    {
                        Database = db,
                        Host = host,
                        Password = password,
                        Username = user,
                        UseSsl = sslType,
                        UseIntegratedSecurity = sslTrustedConnection,
                        AllowSelfSigned = sslSelfSigned
                    };

                    if (portString != null)
                    {
                        var port = Parse.Int(portString);
                        if (!port)
                        {
                            throw new ArgumentException(
                                $"Environment variable ISQEXPLORER_DB_PORT must be a number, was {portString})",
                                port.Exception);
                        }

                        ret.Port = port.Value;
                    }

                    return ret;
                }
                default:
                    throw new InvalidOperationException("shit's fucked");
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddDbContext<ISQExplorerContext>(options => GetConnection().Make(options));

            /*
            services.AddSingleton<ISQExplorerContext>(provider =>
            {
                var options = new DbContextOptionsBuilder<ISQExplorerContext>();
                var newOptions = GetConnection().Make(options);
                return new ISQExplorerContext(newOptions.Options);
            });
            */
            

            // use dependency injection to make our ISQExplorerContext backed by the sql server we choose 
            // services.AddDbContext<ISQExplorerContext>(options => GetConnection().Make(options));
            // also add an instance of our repository to dependency injection
            services.AddScoped<IQueryRepository, QueryRepository>();

            services.AddSingleton<IHtmlClient, HtmlClient>(s =>
                new HtmlClient(() => new RateLimiter(3, 1000)));

            services.AddScoped<ITermRepository, TermRepository>();
            services.AddScoped<IProfessorRepository, ProfessorRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IEntryRepository, EntryRepository>();
            services.AddScoped<Scraper, Scraper>();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "clientapp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}