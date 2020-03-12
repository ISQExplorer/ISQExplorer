#nullable enable
using System;
using ISQExplorer.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ISQExplorer.Database
{
    public class SqlServerConnection : IConnection
    {
        public string? Host { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? Port { get; set; }
        public bool UseSsl { get; set; } 
        public bool AllowSelfSigned { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        private readonly string? _connectionString;
        
        public SqlServerConnection()
        {
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
            
            builder.Encrypt = UseSsl;
            builder.TrustServerCertificate = AllowSelfSigned;
            if (Host != null && Port != null)
            {
                builder.DataSource = $"{Host},{Port}";
            }
            else if (Host != null)
            {
                builder.DataSource = $"{Host}";
                if (Port != null)
                {
                    Print.Error("Warning: Cannot specify port without host in the SQL Server connection string builder.", ConsoleColor.Yellow);
                }
            }
            builder.InitialCatalog = Database;
            builder.IntegratedSecurity = UseIntegratedSecurity;

            return input.UseSqlServer(builder.ConnectionString);
        }
    }
}