using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using URLShortener.Models.DTOs;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class ClickAnalyticsConsumer : BackgroundService
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClickAnalyticsConsumer> _logger;
        private readonly IClickAnalyticsService _clickAnalyticsService;

        public ClickAnalyticsConsumer(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<ClickAnalyticsConsumer> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting Click Analytics Consumer...");
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.GetValue<string>("RABBITMQ_HOST") ?? "localhost",
                    UserName = _configuration.GetValue<string>("RABBITMQ_USER") ?? "user",
                    Password = _configuration.GetValue<string>("RABBITMQ_PASS") ?? "124578"
                };
                _logger.LogInformation("Connecting to RabbitMQ at {Host}...", factory.HostName);

                _connection = await factory.CreateConnectionAsync(cancellationToken);

                _logger.LogInformation("Creating channel...");
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                _logger.LogInformation("Declaring queue...");
                await _channel.QueueDeclareAsync(
                    queue: "url.clicks",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken
                );
                _logger.LogInformation("Setting up consumer...");
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);

                        var clickEvent = JsonSerializer.Deserialize<UrlClickedEvent>(json);

                        if (clickEvent != null)
                        {
                            await ProcessClickEventAsync(clickEvent);
                            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error processing click event");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);

                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: "url.clicks",
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Consumer is now listening!");

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Click Analytics Consumer");
            }
        }

        private async Task ProcessClickEventAsync(UrlClickedEvent clickEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<IClickAnalyticsService>();
            await analyticsService.ProcessClickAsync(clickEvent);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
