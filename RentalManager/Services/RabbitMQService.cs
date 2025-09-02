using RabbitMQ.Client;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace RentalManager.Services;

public interface IRabbitMQService
{
    Task PublishMessageAsync<T>(T data, string operation, string entityType);
    Task InitializeAsync();
}

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IConfiguration _configuration;
    private const string ExchangeName = "rental-manager-exchange";
    private bool _initialized = false;
    private bool _disposed = false;
    private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _lock.WaitAsync();
        try
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
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
        
        _logger.LogInformation("RabbitMQService initialized");
    }

    public async Task PublishMessageAsync<T>(T data, string operation, string entityType)
    {
        try
        {
            if (!_initialized) await InitializeAsync();

            if (_channel is null) throw new Exception($"channel wasn't initialized");

            var message = new OperationMessage<T>
            {
                Operation = operation,
                Data = data,
                TimeStamp = DateTime.Now,
                EntityType = entityType
            };

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties();
            properties.Persistent = true; //message survives the application restart

            var routingKey = $"{entityType}.{operation}";

            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: true, //false: Discard the message if couldn't be routed, true:sends a Basic message otherwise
                basicProperties: properties,
                body: body
            );

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }
    }

    public async void Dispose()
    {
        try
        {
            if (_disposed) return;
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
            if (_channel != null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }
            _lock.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }
        
    }
}

public class OperationMessage<T>
{
    public required string Operation { get; set; }
    public required T Data { get; set; }
    public required DateTime TimeStamp { get; set; }
    public required string EntityType { get; set; }
}