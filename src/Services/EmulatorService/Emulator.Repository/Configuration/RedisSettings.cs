namespace Emulator.Repository.Configuration;

/// <summary>
/// Redis cache configuration settings
/// </summary>
public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultCacheDuration { get; set; } = 3600; // 1 hour in second
}
