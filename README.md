# ISQExplorer
An intelligent way to browse UNF professor ratings.

## Dependencies
All dependencies must be the latest versions.
* .NET Core + NuGet (to run the ASP.NET Core server)
* PostgreSQL or SQL Server (to store the scraped ISQ data)
* npm/yarn + Node.js (to compile the frontend application)

## Getting the project
The easiest way to download the project is using `git`
```shell
$ git clone https://github.com/jonathan-lemos/ISQExplorer
```
If terminals scare you, you can also click "Download as .zip" like a plebian.

## Running the project
Once you have cloned the project, you will need to start up your database server if it is not already running. Once it is, you will need to set the following environment variables so ISQExplorer can connect to your server:
* `ISQEXPLORER_DB_PROVIDER` - `postgres` if you are using a PostgreSQL server, or `sqlserver` if you are using SQL Server (`sqlserver` by default).
* `ISQEXPLORER_DB_HOST` - The IP address of the database server to use (`localhost` by default).
* `ISQEXPLORER_DB_PORT` - The port of the database server to use (`5432` for PostgreSQL and `1433` for SQL Server by default).
* `ISQEXPLORER_DB_DATABASE` - The name of the database to use within the database server (`ISQExplorer` by default). This database should be empty or filled only with ISQExplorer's tables.
* `ISQEXPLORER_DB_USER` - The username to login to the SQL server with (`root` by default). When creating the initial database, this user needs permissions to create tables within the `ISQEXPLORER_DB_DATABASE`. When running the app, this user needs permissions to insert into and read from tables within the `ISQEXPLORER_DB_DATABASE`.
* `ISQEXPLORER_PASSWORD` - The password of the above user. This is an empty string by default.
* `ISQEXPLORER_DB_SSL` - `1` to enable SSL, or `0` to disable it (unset by default).
* `ISQEXPLORER_DB_SSL_ALLOW_SELF_SIGNED` - `1` to allow self-signed certificates with SSL, `0` to disable them (unset by default).
* `ISQEXPLORER_DB_SQLSERVER_TRUSTED_CONNECTION` - `1` to use Windows authentication on SQL Server instead of a username/password, `0` to use a username/password (unset by default).


Once your database server is running and the above environment variables are set, navigate to the project's root directory and type the following
```shell
$ dotnet ef migrations add InitialCreate
$ dotnet ef database update
```
This will initialize the `ISQEXPLORER_DB` database with the tables needed for ISQExplorer.

If you wish to remove these tables, use
```
$ dotnet ef database drop
```

Once the above has been done, you can simply open the solution in your IDE of choice and run the project.

For best results, use Visual Studio on Windows and Rider on OSX/Linux.

## Contributing guidelines
See [CONTRIBUTING.md](CONTRIBUTING.md).
