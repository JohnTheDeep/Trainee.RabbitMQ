namespace Trainee.PostOffice.Configuration;

public record RabbitMQConfiguration
{
    public string? ClientProviderName { get; set; }
    public string? HostNames { get; set; }
    public string? Port { get; set; }
    public string? VirtualHost { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
