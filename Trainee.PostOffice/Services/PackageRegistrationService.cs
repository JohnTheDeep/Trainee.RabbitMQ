using System.Text.Json;
using Trainee.PostOffice.Models;
using Trainee.PostOffice.Models.Enums;

namespace Trainee.PostOffice.Services;

public class PackageRegistrationService(ILogger<PackageRegistrationService> logger, RabbitMQPublisher rabbitMQPublisher)
{
    private readonly ILogger<PackageRegistrationService> _logger = logger;
    private readonly RabbitMQPublisher _rabbitMQPublisher = rabbitMQPublisher;

    public async Task RegisterPackage(Package package)
    {
        _logger.LogInformation("Start registration of package {@packageGuid}", package.PackageGuid);
        package.RegisteredAt = DateTime.UtcNow;
        package.Status = PackageStatus.Registered;
        await Task.Delay(5000); //Imitation of some business logic
        await _rabbitMQPublisher.Publish("RegisteredPackages", JsonSerializer.Serialize(package));
        _logger.LogInformation("Registration ended");
    }
}
