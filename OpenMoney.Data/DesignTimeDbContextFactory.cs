using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenMoney.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=openmoney;User=root;Password=;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        return new AppDbContext(opts);
    }
}
