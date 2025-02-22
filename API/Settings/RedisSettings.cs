namespace API.Settings;

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectTimeout { get; set; }
    public int ConnectRetry { get; set; }
}