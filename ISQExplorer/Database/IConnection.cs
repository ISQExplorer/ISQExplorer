using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Database
{
    /// <summary>
    /// Sets options for the creation of a DbContext.
    /// Use this to choose which type of database to use.
    /// </summary>
    public interface IConnection
    {
        DbContextOptionsBuilder Make(DbContextOptionsBuilder input);
    }
}