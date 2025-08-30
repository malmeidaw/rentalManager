using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MotorbikeConsumer.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RentalConsumer.Services;

public class RentalConsumerService : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;
    private readonly ILogger<RentalConsumerService> _logger;
    private const string ExchangeName = "rental-manager-exchange";
    private const string QueueName = "rental-queue";
    private const string RequestQueueName = "rental-requests";
    private bool _initialized = false;
    public RentalConsumerService(IConfiguration configuration,
                                 IServiceProvider serviceProvider,
                                 ILogger<RentalConsumerService> logger)
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
        await _channel.QueueBindAsync(QueueName, ExchangeName, "rental.create");
        //////
        await _channel.QueueDeclareAsync(RequestQueueName, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "rental.update");
        await _channel.QueueBindAsync(RequestQueueName, ExchangeName, "rental.getbyid");

        await _channel.BasicQosAsync(0, 1, false);
        _initialized = true;
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
                var operationMessage = JsonSerializer.Deserialize<OperationMessage<Rental>>(message);

                using var scope = _serviceProvider.CreateScope();
                var rentalService = scope.ServiceProvider.GetRequiredService<IRentalService>();

                switch (operationMessage?.Operation)
                {
                    case "create":
                        await rentalService.CreateRentalAsync(operationMessage.Data);
                        break;
                    // case "update":
                        // await rentalService.UpdateRentalAsync(operationMessage.Data);
                        // break;
                    // case "delete":
                    // await RentalService.DeleteRentalAsync(operationMessage.Data);
                    // break;
                    default:
                        _logger.LogError($"no command {operationMessage?.Operation}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            await Task.Yield();
        };

        var requestConsumer = new AsyncEventingBasicConsumer(_channel);
        requestConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var requestMessage = JsonSerializer.Deserialize<RequestMessage>(message);
                if (requestMessage == null)
                {
                    _logger.LogError("request message couldn't be read");
                    Console.WriteLine("request message couldn't be read");
                    throw new Exception("request message couldn't be read");
                }

                using var scope = _serviceProvider.CreateScope();
                var rentalService = scope.ServiceProvider.GetRequiredService<IRentalService>();
                object? responseData = null;
                bool success = true;
                string? error = null;

                switch (requestMessage?.Operation?.ToLower())
                {
                    // case "get":
                    // var Rentals = await RentalService.GetRentalAsync();
                    // responseData = Rentals;
                    // break;
                    case "getbyid":
                        string? id = null;

                        if (requestMessage.Data is JsonElement jsonElement &&
                            jsonElement.ValueKind == JsonValueKind.String)
                        {
                            id = jsonElement.GetString();      
                        }
                        else
                        {
                            success = false;
                            error = $"{id} not sent";
                        }

                        var rental = await rentalService.GetRentalAsync(id);
                        if (rental != null) responseData = rental;
                        else
                        {
                            success = false;
                            error = $"{id} not found";
                        }
                        break;
                    case "update":
                        // Rental? r = (Rental?)requestMessage.Data;
                        try
                        {
                            JsonDocument asJson = JsonDocument.Parse(requestMessage?.Data.ToString());
                            DateTime? eed = null;
                            var updatedRentalData = JsonSerializer.Deserialize<UpdateRentalData>(asJson);
                            if (updatedRentalData != null)
                            {
                                _logger.LogInformation($"{updatedRentalData.Id} {updatedRentalData.ExpectedEndDate}");
                                eed = DateTime.TryParse(updatedRentalData.ExpectedEndDate, out var eed_) ? eed_ : null;
                            }
                            if (eed != null)
                            {
                                DateTime expectedEndDate = eed ?? DateTime.Now;
                                responseData = await rentalService.UpdateRentalAsync(updatedRentalData.Id, expectedEndDate);
                            }
                            else
                            {
                                _logger.LogError("Couldn't read new expected end date");
                                Console.WriteLine("Couldn't read new expected end date");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"{ex.Message}");
                            Console.WriteLine($"{ex.Message}");
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
                    CorrelationId = correlationId,
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
                Console.WriteLine($"{ex.Message}");
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
                    Console.WriteLine($"{ex_.Message}");
                }
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };
        await _channel.BasicConsumeAsync(QueueName, false, consumer);
        await _channel.BasicConsumeAsync(RequestQueueName, false, requestConsumer);

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
    public string? CorrelationId { get; set; } //check if it can be null
    public string? ReplyTo { get; set; }
}

public class ResponseMessage
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public string? CorrelationId { get; set; }
}

public class UpdateRentalData
{
    public string? Id { get; set;}
    public string? ExpectedEndDate { get; set;}
}