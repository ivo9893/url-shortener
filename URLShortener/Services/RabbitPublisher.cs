using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System.Drawing.Printing;
using System.Text;
using System.Text.Json;
using URLShortener.Models.DTOs;
using URLShortener.Services.Interfaces;

namespace URLShortener.Services
{
    public class RabbitPublisher : IRabbitPublisher, IHostedService, IAsyncDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitPublisher> _logger;

        public RabbitPublisher(IConfiguration configuration, ILogger<RabbitPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            var factory = new ConnectionFactory
            {
                HostName = _configuration.GetValue<string>("RABBITMQ_HOST") ?? "localhost",
                UserName = _configuration.GetValue<string>("RABBITMQ_USER") ?? "user",
                Password = _configuration.GetValue<string>("RABBITMQ_PASS") ?? "124578"
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken : cancellationToken);

            await _channel.QueueDeclareAsync(
                queue: "url.click",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ connection established!!");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisposeAsync();
        }

        public async Task PublishClickEvent(UrlClickedEvent clickEvent)
        {
            if(_channel == null)
            {
                _logger.LogWarning("Cannot publish - RabbitMQ not initialized");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(clickEvent);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "url.clicks",
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Published click event for {ShortCode}", clickEvent.ShortCode);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish click event");
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}
