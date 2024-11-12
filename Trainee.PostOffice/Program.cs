using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Trainee.PostOffice.BackgroundServices;
using Trainee.PostOffice.Configuration;
using Trainee.PostOffice.Services;

var logger = LogManager.Setup().GetCurrentClassLogger();

try
{
    logger.Info("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var configuration = builder.Configuration;
    var services = builder.Services;
    services.AddHealthChecks();
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(o =>
    {
        o.SupportNonNullableReferenceTypes();
        o.UseInlineDefinitionsForEnums();
        foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
            o.IncludeXmlComments(xmlFile);

        var securityScheme = new OpenApiSecurityScheme
        {
            Scheme = "Bearer",
            Type = SecuritySchemeType.Http,
            In = ParameterLocation.Header,
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Authorization"
            }
        };
        o.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });
    services.AddSingleton<PackageRegistrationService>();
    services.AddSingleton(_ =>
    {
        var logger = _.GetRequiredService<ILogger<RabbitMQPublisher>>();
        var config = _.GetRequiredService<IOptions<RabbitMQConfiguration>>();
        var publisher = new RabbitMQPublisher(logger, config);
        Task.Run(async () => await publisher.InitializeAsync()).GetAwaiter().GetResult();
        return publisher;
    });
    services.AddSingleton(_ =>
    {
        var logger = _.GetRequiredService<ILogger<RabbitMQConsumer>>();
        var config = _.GetRequiredService<IOptions<RabbitMQConfiguration>>();
        var consumer = new RabbitMQConsumer(logger, config);
        Task.Run(async () => await consumer.InitializeAsync()).GetAwaiter().GetResult();
        return consumer;
    });
    services.AddHostedService<PackagesBackgroundService>();
    services.Configure<RabbitMQConfiguration>(configuration.GetSection("RabbitMq"));

    var app = builder.Build();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.UseHealthChecks("/healthz");
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.Run();

}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
