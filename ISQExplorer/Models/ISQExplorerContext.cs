using System;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public enum TermSeason
    {
        Spring = 0,
        Summer = 1,
        Fall = 2
    }

    public static class TermMethods
    {
        public static string ToString(this Term term, int? year = null)
        {
            if (year == null)
            {
                return term switch
                {
                    Term.Spring => "Spring",
                    Term.Summer => "Summer",
                    Term.Fall => "Fall",
                    _ => throw new ArgumentException("Term was invalid")
                };
            }

            return $"{term.ToString()} {year}";
        }

        public static int CompareTerms((Term, int) o1, (Term, int) o2)
        {
            
        }

        public static (Term SinceTerm, int SinceYear) ParseTerm(string entry)
        {
            var since = entry.Split(" ");
            if (since.Length != 2)
            {
                throw new DataException($"Invalid since field '{entry}'");
            }

            var term = since[0] switch
            {
                "Spring" => Term.Spring,
                "Summer" => Term.Summer,
                "Fall" => Term.Fall,
                _ => throw new DataException($"Invalid term '{since[0]}'")
            };
            
            if (!int.TryParse(since[1], out var year))
            {
                throw new DataException($"Invalid year '{since[1]}'");
            }

            return (term, year);
        }

        public static (Term? SinceTerm, int? SinceYear) ParseTermNullable(string? entry)
        {
            if (entry == null)
            {
                return (null, null);
            }

            return ParseTerm(entry);
        }
    }

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
        }
    }
}