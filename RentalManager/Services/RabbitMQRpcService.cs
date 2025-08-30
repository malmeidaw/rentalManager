using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace RentalManager.Services;

public interface IRabbitMQRpcService
{
    Task<T> SendRequestAsync<T>(string operation, object? data, string? entityType);
    Task InitializeAsync();
}

public class RabbitMQRpcService : IRabbitMQRpcService, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private IConfiguration _configuration;
    private string? _replyQueueName;
    private AsyncDictionary<string, TaskCompletionSource<string>> _pendingRequests;
    private const string ExchangeName = "rental-manager-exchange";
    private bool _initialized = false;
    private bool _disposed = false;
    private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public RabbitMQRpcService(IConfiguration configuration)
    {
        _configuration = configuration;
        _pendingRequests = new AsyncDictionary<string, TaskCompletionSource<string>>();
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
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).Wait();
            _replyQueueName = _channel.QueueDeclareAsync(exclusive: true).Result.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var correlationId = ea.BasicProperties.CorrelationId;
                if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryRemove(correlationId, out var tcs))
                {
                    // _pendingRequests.Remove(correlationId);
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    if (tcs != null) tcs.TrySetResult(response);
                }
                await Task.Yield();
            };
            _channel.BasicConsumeAsync(_replyQueueName, true, consumer).GetAwaiter().GetResult();
            _initialized = true;
        }
        finally { _lock.Release(); }
    }

    public async Task<T> SendRequestAsync<T>(string operation, object? data, string? entityType)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();
        _pendingRequests.Add(correlationId, tcs);

        if (!_initialized) await InitializeAsync();
        if (_channel is null) throw new Exception($"channel wasn't initialized");
        if (_replyQueueName is null) throw new Exception($"replyQueue was not assigned");

        try
        {
            var requestMessage = new RequestMessage
            {
                Operation = operation,
                Data = data,
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName
            };

            var _ = JsonSerializer.Serialize(requestMessage);
            var body = Encoding.UTF8.GetBytes(_);

            var properties = new BasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = _replyQueueName;

            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: $"{entityType}.{operation}",
                mandatory: true,
                basicProperties: properties,
                body: body
            );
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(32));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _pendingRequests.Remove(correlationId);
                throw new Exception("timed out");
            }
            var responseJson = await tcs.Task;
            var response = JsonSerializer.Deserialize<ResponseMessage>(responseJson);
            if (response == null || !response.Success) { throw new Exception(response?.Error ?? "unknown"); }
            var responseDeserialized = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(response.Data));
            if (responseDeserialized != null) return responseDeserialized;
            else throw new Exception("No response received");
        }
        catch (Exception ex)
        {
            _pendingRequests.Remove(correlationId);
            Console.WriteLine($"{ex.Message}");
            throw;
        }
    }
    public async void Dispose()
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
}

public class ResponseMessage
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public string? CorrelationId { get; set; }
}

public class RequestMessage
{
    public required string Operation { get; set; }
    public object? Data { get; set; }
    public required string CorrelationId { get; set; } //check if it can be null
    public required string ReplyTo { get; set; }
}

public class AsyncDictionary<TKey, TValue> where TKey : notnull//read it better
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();
    private readonly object _lock = new();
    public void Add(TKey key, TValue value)
    {
        lock (_lock) _dictionary.Add(key, value);
    }
    public bool TryRemove(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_dictionary.TryGetValue(key, out value))
            {
                _dictionary.Remove(key);
                return true;
            }
            return false;
        }
    }
    public bool Remove(TKey key)
    {
        lock (_lock)
            return _dictionary.Remove(key);
    }
}
