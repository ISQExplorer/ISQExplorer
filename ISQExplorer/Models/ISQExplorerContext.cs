using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ISQExplorerContext : DbContext
    {
        public ISQExplorerContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ISQEntryModel> IsqEntries { get; set; }
        public DbSet<ProfessorModel> Professors { get; set; }
        public DbSet<QueryModel> Queries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProfessorModel>()
                .HasAlternateKey(c => c.NNumber);
        }
    }
}