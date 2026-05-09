using Microsoft.Extensions.Configuration;
using URLShortener.Services;

namespace URLShortener.Tests;

public class IdGeneratorServiceTest
{

    private IdGeneratorService CreateGenerator(int workerId = 1)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"SNOWFLAKE_WORKER_ID", workerId.ToString() }
            }).Build();

        return new IdGeneratorService(config);
    }

    [Fact]
    public void GenerateId_ShouldReturnPositiveNumber()
    {
        var generator = CreateGenerator();

        var id = generator.GenerateId();

        Assert.True(id > 0);
    }


    [Fact]
    public void GenerateShortCode_ShouldReturnNonEmptyString()
    {
        var generator = CreateGenerator();
        var shortCode = generator.GenerateShortCode();
        Assert.False(string.IsNullOrEmpty(shortCode.shortCode));
    }

    [Fact]
    public void GenerateShortCode_ShouldReturnUniqueCodes()
    {
        var generator = CreateGenerator();
        var codes = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            var code = generator.GenerateShortCode();
            Assert.False(codes.Contains(code.shortCode), $"Duplicate code generated: {code.shortCode}");
            codes.Add(code.shortCode);
        }
    }

    [Fact]
    public void GenerateId_ShouldThrowException_WhenClockMovesBackwards()
    {
        var generator = CreateGenerator();
        // Simulate clock moving backwards by setting last timestamp to future
        var lastTimestampField = typeof(IdGeneratorService).GetField("_lastTimestamp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lastTimestampField.SetValue(generator, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1000);
        Assert.Throws<InvalidOperationException>(() => generator.GenerateId());
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenWorkerIdIsNegative()
    {
        Assert.Throws<ArgumentException>(() => CreateGenerator(-1));
    }
}
