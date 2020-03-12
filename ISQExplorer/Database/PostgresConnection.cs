#nullable enable
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ISQExplorer.Database
{
    public class PostgresConnection : IConnection
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public SslMode SSLType { get; set; }
        public bool AllowSelfSigned { get; set; }
        private readonly string? _connectionString;

        public PostgresConnection()
        {
            (Host, Database, Username, Password, Port, SSLType) = ("localhost", nameof(ISQExplorer), "root", "", 5432,
                SslMode.Prefer);
        }

        public PostgresConnection(string connString)
        {
            _connectionString = connString;
        }

        public DbContextOptionsBuilder Make(DbContextOptionsBuilder input)
        {
            if (_connectionString != null)
            {
                return input.UseNpgsql(_connectionString);
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = Database,
                Username = Username,
                Password = Password,
                SslMode = SSLType,
                TrustServerCertificate = AllowSelfSigned
            };

            return input.UseNpgsql(builder.ConnectionString);
        }
    }
}