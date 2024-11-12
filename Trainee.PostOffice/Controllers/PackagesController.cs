using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Trainee.PostOffice.Models;
using Trainee.PostOffice.Services;

namespace Trainee.PostOffice.Controllers;

[Route("api/v1/[controller]")]
public class PackagesController(RabbitMQConsumer consumer, RabbitMQPublisher publisher, ILogger<PackagesController> logger) : Controller
{
    private readonly RabbitMQConsumer _consumer = consumer;
    private readonly RabbitMQPublisher _publisher = publisher;
    private readonly ILogger<PackagesController> _logger = logger;

    /// <summary>
    /// Метод для занесения в очередь посылок на регистрацию
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    public async Task CreatePackage([FromBody] Package model)
    {
        await _publisher.Publish("PackagesForRegistration", JsonSerializer.Serialize(model));
    }
}
