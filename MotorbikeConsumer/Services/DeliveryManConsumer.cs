using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeliveryManConsumer.Services;

public class DeliveryManConsumerService : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;
    private readonly ILogger<DeliveryManConsumerService> _logger;
    private const string ExchangeName = "rental-manager-exchange";
    private const string QueueName = "delivery-man-queue";
    private bool _initialized = false;
    public DeliveryManConsumerService(IConfiguration configuration, IServiceProvider serviceProvider,
                                      ILogger<DeliveryManConsumerService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "delivery-man.create");
        // await _channel.QueueBindAsync(QueueName, ExchangeName, "delivery-man.update");
        // await _channel.QueueBindAsync(QueueName, ExchangeName, "delivery-man.delete");
        //////
        // await _channel.QueueDeclareAsync(RequestQueueName, durable: true, exclusive: false, autoDelete: false);
        // await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "deliveryMan.get");
        // await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "deliveryMan.getbyid");

        await _channel.BasicQosAsync(0, 1, false);
        _initialized = true;
        _logger.LogInformation("DeliveryManConsumerService initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        if (!_initialized) await InitializeAsync();

        if (_channel is null) {
            _logger.LogError("Channel wasn't initialized");
            throw new Exception($"channel wasn't initialized");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var operationMessage = JsonSerializer.Deserialize<OperationMessage<DeliveryMan>>(message);

                using var scope = _serviceProvider.CreateScope();
                var deliveryManService = scope.ServiceProvider.GetRequiredService<IDeliveryManService>();

                switch (operationMessage?.Operation)
                {
                    case "create":
                        await deliveryManService.CreateDeliveryManAsync(operationMessage.Data);
                        break;
                    // case "update":
                    // await deliveryManService.UpdateDeliveryManAsync(operationMessage.Data);
                    // break;
                    // case "delete":
                    // await deliveryManService.DeleteDeliveryManAsync(operationMessage.Data);
                    // break;
                    default:
                        _logger.LogError($"no command {operationMessage?.Operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                Console.WriteLine($"{ex.Message}");
            }
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(QueueName, false, consumer);
    }
    public override async void Dispose()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        base.Dispose();
        _logger.LogInformation("DeliveryManConsumerService disposed");
    }
}
//duplicated code
public class OperationMessage<T>
{
    public required string Operation { get; set; }
    public required T Data { get; set; }
    public required DateTime TimeStamp { get; set; }
    public required string EntityType { get; set; }
}

public class RequestMessage
{
    public required string Operation { get; set; }
    public object? Data { get; set; }
    public required string CorrelationId { get; set; }
    public required string ReplyTo { get; set; }
}

public class ResponseMessage
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public required string CorrelationId { get; set; }
}
