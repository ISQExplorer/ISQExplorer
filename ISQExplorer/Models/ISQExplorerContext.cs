using System;
using System.Data;
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
        public DbSet<CourseModel> Courses { get; set; }
        public DbSet<CourseNameModel> CourseNames { get; set; }
        public DbSet<CourseCodeModel> CourseCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseModel>()
                .HasIndex(c => c.Description);
            modelBuilder.Entity<ProfessorModel>()
                .HasIndex(c => c.NNumber);
            modelBuilder.Entity<ISQEntryModel>()
                .HasIndex(c => new {c.Crn, c.Season, c.Year});
        }
    }
}