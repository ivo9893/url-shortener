using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using URLShortener.Infrastructure;

namespace URLShortener.Tests
{
    public class ShardRouteTests
    {
        private ShardRouter CreateRouter()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Sharding:VirtualToPhysicalMap:0", "0" },
                    { "Sharding:VirtualToPhysicalMap:1", "0" },
                    { "Sharding:VirtualToPhysicalMap:2", "0" },
                    { "Sharding:VirtualToPhysicalMap:3", "0" },
                    { "Sharding:VirtualToPhysicalMap:4", "1" },
                    { "Sharding:VirtualToPhysicalMap:5", "1" },
                    { "Sharding:VirtualToPhysicalMap:6", "1" },
                    { "Sharding:VirtualToPhysicalMap:7", "1" },
                    { "Sharding:VirtualToPhysicalMap:8", "2" },
                    { "Sharding:VirtualToPhysicalMap:9", "2" },
                    { "Sharding:VirtualToPhysicalMap:10", "2" },
                    { "Sharding:VirtualToPhysicalMap:11", "2" },
                    { "Sharding:VirtualToPhysicalMap:12", "3" },
                    { "Sharding:VirtualToPhysicalMap:13", "3" },
                    { "Sharding:VirtualToPhysicalMap:14", "3" },
                    { "Sharding:VirtualToPhysicalMap:15", "3" },
                    { "SHARD_0_CONNECTION", "Server=shard0;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_1_CONNECTION", "Server=shard1;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_2_CONNECTION", "Server=shard2;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_3_CONNECTION", "Server=shard3;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_4_CONNECTION", "Server=shard4;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_5_CONNECTION", "Server=shard5;Database=UrlShortener;User Id=sa;Password=your_password;" },
                    { "SHARD_6_CONNECTION", "Server=shard6;Database=UrlShortener;User Id=sa;Password=your_password;" },
                }).Build();

            return new ShardRouter(config, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ShardRouter>());
        }

        [Fact]
        public void GetShardConnectionString_ShouldReturnValidConnectionString()
        {
            // Arrange
            var router = CreateRouter();

            // Act
            var conn = router.GetShardConnectionString("abc123X");

            // Assert
            Assert.NotNull(conn);
            Assert.StartsWith("Server=", conn);
        }

        [Fact]
        public void GetShardConnectionString_SameCodeShouldAlwaysReturnSameShard()
        {
            // Arrange
            var router = CreateRouter();
            string shortCode = "xK9pQ2z";

            // Act
            var conn1 = router.GetShardConnectionString(shortCode);
            var conn2 = router.GetShardConnectionString(shortCode);

            // Assert
            Assert.Equal(conn1, conn2);
        }

        [Fact]
        public void GetShardConnectionString_DifferentCodesShouldReturnDifferentShards()
        {
            // Arrange
            var router = CreateRouter();
            string code1 = "abc123X";
            string code2 = "def456Y";
            // Act
            var conn1 = router.GetShardConnectionString(code1);
            var conn2 = router.GetShardConnectionString(code2);
            // Assert
            Assert.NotEqual(conn1, conn2);


        }
    }
      
}
