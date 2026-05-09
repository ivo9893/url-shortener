using Microsoft.EntityFrameworkCore;
using URLShortener.Data;

namespace URLShortener.Infrastructure
{
    public class ShardedDbContextFactory
    {
        private readonly ShardRouter _shardRouter;

        public ShardedDbContextFactory(ShardRouter shardRouter)
        {
            _shardRouter = shardRouter;
        }

        public ApplicationDbContext CreateDbContext(string shortCode) 
        {
            string connectionString = _shardRouter.GetShardConnectionString(shortCode);

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
