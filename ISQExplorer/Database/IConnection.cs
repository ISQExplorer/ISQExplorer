using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Database
{
    public interface IConnection
    {
        DbContextOptionsBuilder Make(DbContextOptionsBuilder input);
    }
}