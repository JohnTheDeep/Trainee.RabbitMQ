
using System.Text.Json;
using Trainee.PostOffice.Models;
using Trainee.PostOffice.Services;

namespace Trainee.PostOffice.BackgroundServices;

/// <summary>
/// Фоновый сервис для получения посылок на регистрацию, получение зарегистрированных посылок.
/// </summary>
public class PackagesBackgroundService : BackgroundService
{
    private readonly RabbitMQConsumer _rabbitConsumer;
    private readonly ILogger<PackagesBackgroundService> _logger;
    private readonly PackageRegistrationService _registrationService;

    public PackagesBackgroundService(
        RabbitMQConsumer rabbitConsumer,
        ILogger<PackagesBackgroundService> logger,
        PackageRegistrationService registrationService)
    {
        _rabbitConsumer = rabbitConsumer;
        _logger = logger;
        _registrationService = registrationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            await _rabbitConsumer.Consume("PackagesForRegistration", async packageForRegistration =>
            {
                var package = JsonSerializer.Deserialize<Package>(packageForRegistration);
                await _registrationService.RegisterPackage(package!);
            });

            await _rabbitConsumer.Consume("RegisteredPackages", async registeredPackage =>
            {
                await Task.Delay(1000);
                var package = JsonSerializer.Deserialize<Package>(registeredPackage);
                _logger.LogInformation("Package from registration queue was received {@package}", registeredPackage);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed execute");
        }
    }
}
