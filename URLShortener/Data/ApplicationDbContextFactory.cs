using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace URLShortener.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use shard 0 for migrations (they all have the same schema anyway)
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=url_shortener;Username=postgres;Password=124578");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
