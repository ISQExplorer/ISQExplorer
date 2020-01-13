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
        public DbSet<CourseModel> Courses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseModel>()
                .HasIndex(c => c.CourseCode);
            modelBuilder.Entity<DepartmentModel>()
                .HasIndex(c => c.Name);
            modelBuilder.Entity<ISQEntryModel>()
                .HasIndex(c => new {c.Crn, c.Season, c.Year});
            modelBuilder.Entity<ProfessorModel>()
                .HasIndex(c => c.NNumber);
        }
    }
}