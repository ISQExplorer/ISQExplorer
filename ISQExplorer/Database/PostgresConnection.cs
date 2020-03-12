#nullable enable
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ISQExplorer.Database
{
    public class PostgresConnection : IConnection
    {
        public string? Host { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; }
        public SslMode? SslType { get; set; }
        public bool AllowSelfSigned { get; set; }
        private readonly string? _connectionString;

        public PostgresConnection()
        {
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
                Database = Database,
                Username = Username,
                Password = Password,
                TrustServerCertificate = AllowSelfSigned
            };
            
            if (Port != null)
                builder.Port = (int) Port;
            if (SslType != null)
                builder.SslMode = (SslMode) SslType;

            return input.UseNpgsql(builder.ConnectionString);
        }
    }
}