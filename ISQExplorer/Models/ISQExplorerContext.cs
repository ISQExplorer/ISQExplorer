using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ISQExplorerContext : DbContext
    {
        public ISQExplorerContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<DepartmentModel> Departments { get; set; } = null!;
        public DbSet<ISQEntryModel> IsqEntries { get; set; } = null!;
        public DbSet<ProfessorModel> Professors { get; set; } = null!;
        public DbSet<CourseModel> Courses { get; set; } = null!;
        public DbSet<TermModel> Terms { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // you cannot specify any FK's as indices
            // you will get a very cryptic error if you do
            
            modelBuilder.Entity<CourseModel>()
                .HasIndex(c => c.CourseCode)
                .IsUnique();
            modelBuilder.Entity<DepartmentModel>()
                .HasIndex(c => c.Name)
                .IsUnique();
            modelBuilder.Entity<ProfessorModel>()
                 .HasIndex(c => c.NNumber)
                 .IsUnique();
            modelBuilder.Entity<TermModel>()
                 .HasIndex(c => c.Name)
                 .IsUnique();

            modelBuilder.Entity<ProfessorModel>()
                .HasMany<ISQEntryModel>()
                .WithOne(x => x.Professor)
                .OnDelete(DeleteBehavior.NoAction);


            /*
            modelBuilder.Entity<ISQEntryModel>()
                .HasOne<TermModel>();
                */
        }
    }
}