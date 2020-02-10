#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ISQExplorer.Database
{
    public class SqlServerConnection : IConnection
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
        public bool AllowSelfSigned { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        private readonly string? _connectionString;


        public SqlServerConnection()
        {
            (Host, Database, Username, Password, Port, UseSSL, AllowSelfSigned, UseIntegratedSecurity) = ("localhost",
                nameof(ISQExplorer), "root", "", 1433, false, false, true);
        }

        public SqlServerConnection(string connString)
        {
            _connectionString = connString;
        }

        public DbContextOptionsBuilder Make(DbContextOptionsBuilder input)
        {
            if (_connectionString != null)
            {
                return input.UseSqlServer(_connectionString);
            }

            var builder = new SqlConnectionStringBuilder();
            if (!UseIntegratedSecurity)
            {
                builder.UserID = Username;
                builder.Password = Password;
            }

            builder.Encrypt = UseSSL;
            builder.TrustServerCertificate = AllowSelfSigned;
            builder.DataSource = $"Host,{Port}";
            builder.InitialCatalog = Database;
            builder.IntegratedSecurity = UseIntegratedSecurity;

            return input.UseSqlServer(builder.ConnectionString);
        }
    }
}