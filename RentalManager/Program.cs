using RentalManager.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    {
        var swaggerConfig = builder.Configuration.GetSection("Swagger");
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentalManager API", Version = "v1" });
        c.AddServer(new OpenApiServer { Url = swaggerConfig["BaseUrl"] ?? "http://localhost:5289" });
    }
);


builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddSingleton<IRabbitMQRpcService, RabbitMQRpcService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
    await rabbitMQService.InitializeAsync();
}

using (var scope = app.Services.CreateScope())
{
    var rabbitMQRpcService = scope.ServiceProvider.GetRequiredService<IRabbitMQRpcService>();
    await rabbitMQRpcService.InitializeAsync();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

try { await app.RunAsync(); }
catch (Exception ex) { Console.WriteLine($"{ex.Message}"); }