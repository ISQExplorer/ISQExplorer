using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorerTests
{
    public static class Mock
    {
        public static ISQExplorerContext DbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ISQExplorerContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ISQExplorerContext(options);
        }
    }
}