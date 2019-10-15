using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Models
{
    public class ISQExplorerContext : DbContext
    {
        public ISQExplorerContext(DbContextOptions options) : base(options)
        {
        }
        
        public DbSet<IsqEntryModel> IsqEntries { get; set; }
        public DbSet<ProfessorModel> Professors { get; set; }
    }
}