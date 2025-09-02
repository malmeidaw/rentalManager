using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MotorbikeConsumer.Services;

public class MotorbikeConsumerService : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;
    private readonly ILogger<MotorbikeConsumerService> _logger;
    private const string ExchangeName = "rental-manager-exchange";
    private const string QueueName = "motorbike-queue";
    private const string RequestQueueName = "motorbike-requests";
    private bool _initialized = false;
    public MotorbikeConsumerService(IConfiguration configuration, IServiceProvider serviceProvider,
                                    ILogger<MotorbikeConsumerService> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public async Task InitializeAsync()
    {
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

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, "motorbike.create");
            await _channel.QueueBindAsync(QueueName, ExchangeName, "motorbike.update");
            await _channel.QueueBindAsync(QueueName, ExchangeName, "motorbike.delete");
            await _channel.QueueBindAsync(QueueName, ExchangeName, "motorbike.is2024");
            //////
            await _channel.QueueDeclareAsync(RequestQueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "motorbike.get");
            // await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "motorbike.getbyid");
            await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "motorbike.getbyplate");

            await _channel.BasicQosAsync(0, 1, false);
            _initialized = true;
            _logger.LogInformation("DeliveryManConsumerService initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        if (!_initialized) await InitializeAsync();

        if (_channel is null) throw new Exception($"channel wasn't initialized");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var operationMessage = JsonSerializer.Deserialize<OperationMessage<Motorbike>>(message);

                using var scope = _serviceProvider.CreateScope();
                var motorbikeService = scope.ServiceProvider.GetRequiredService<IMotorbikeService>();

                switch (operationMessage?.Operation)
                {
                    case "create":
                        await motorbikeService.CreateMotorbikeAsync(operationMessage.Data);
                        break;
                    case "update":
                        await motorbikeService.UpdateMotorbikeAsync(operationMessage.Data);
                        break;
                    case "delete":
                        await motorbikeService.DeleteMotorbikeAsync(operationMessage.Data);
                        break;
                    case "is2024":
                        motorbikeService.Notify2024(operationMessage.Data);
                        break;
                    default:
                        _logger.LogError($"no command {operationMessage?.Operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
            }
            try
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                await Task.Yield();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
            }
        };

        var requestConsumer = new AsyncEventingBasicConsumer(_channel);
        requestConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var requestMessage = JsonSerializer.Deserialize<RequestMessage>(message);

                using var scope = _serviceProvider.CreateScope();
                var motorbikeService = scope.ServiceProvider.GetRequiredService<IMotorbikeService>();
                object? responseData = null;
                bool success = true;
                string? error = null;

                switch (requestMessage?.Operation?.ToLower())
                {
                    case "get":
                        var motorbikes = await motorbikeService.GetMotorbikeAsync();
                        responseData = motorbikes;
                        break;
                    case "getbyid":
                        string? id = (requestMessage.Data as JsonElement?).ToString();
                        var mi = await motorbikeService.GetMotorbikeAsync(id);
                        if (mi != null) responseData = mi;
                        else
                        {
                            success = false;
                            error = $"{id} not found";
                        }
                        break;
                    case "getbyplate":
                        string? plate = (requestMessage.Data as JsonElement?).ToString();
                        var mp = await motorbikeService.GetMotorbikeByPlateAsync(plate);
                        if (mp != null) responseData = mp;
                        else
                        {
                            success = false;
                            error = $"{plate} not found";
                        }
                        break;
                    default:
                        success = false;
                        _logger.LogError($"no command {requestMessage?.Operation}");
                        error = $"no command {requestMessage?.Operation}";
                        break;
                }

                var correlationId = requestMessage?.CorrelationId;
                var replyTo = requestMessage?.ReplyTo;
                if (correlationId == null || replyTo == null)
                {
                    _logger.LogError($"requestMessage does not have all attributes necessary\n{correlationId} {replyTo} ");
                    throw new Exception($"requestMessage does not have all attributes necessary\n{correlationId} {replyTo} ");
                }

                var response = new ResponseMessage
                {
                    Success = success,
                    Data = responseData,
                    Error = error,
                    CorrelationId = correlationId
                };

                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                var properties = new BasicProperties();
                properties.Persistent = false;
                properties.CorrelationId = requestMessage?.CorrelationId;

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: replyTo,
                    mandatory: true,
                    basicProperties: properties,
                    body: responseBody
                );
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                try
                {
                    var _ = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var requestMessage = JsonSerializer.Deserialize<RequestMessage>(_);
                    var correlationId = requestMessage?.CorrelationId;
                    var replyTo = requestMessage?.ReplyTo;
                    if (correlationId == null || replyTo == null)
                    {
                        _logger.LogError($"requestMessage does not have all attributes necessary\n{correlationId} {replyTo} ");
                        throw new Exception($"requestMessage does not have all attributes necessary\n{correlationId} {replyTo} ");
                    }
                    var errorResponse = new ResponseMessage
                    {
                        Success = false,
                        Error = ex.Message,
                        CorrelationId = correlationId
                    };
                    var responseJson = JsonSerializer.Serialize(errorResponse);
                    var responseBody = Encoding.UTF8.GetBytes(responseJson);
                    var properties = new BasicProperties();
                    properties.CorrelationId = requestMessage?.CorrelationId;
                    properties.Persistent = false;
                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: replyTo,
                        mandatory: true,
                        basicProperties: properties,
                        body: responseBody
                    );
                }
                catch (Exception ex_)
                {
                    _logger.LogError($"{ex_.Message}");
                }
                try
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex__)
                {
                    _logger.LogError($"{ex__.Message}");
                }
            }
        };
        try
        {
            await _channel.BasicConsumeAsync(QueueName, false, consumer);
            await _channel.BasicConsumeAsync(RequestQueueName, false, requestConsumer);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
        }

    }
    public override async void Dispose()
    {
        try
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
            _logger.LogInformation("MotorbikeConsumerService disposed");
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
