using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concoction.Infrastructure.Persistence;

public sealed class ConcoctionDbContextFactory : IDesignTimeDbContextFactory<ConcoctionDbContext>
{
    public ConcoctionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConcoctionDbContext>();
        optionsBuilder.UseSqlite("Data Source=design-time.db");
        return new ConcoctionDbContext(optionsBuilder.Options);
    }
}
