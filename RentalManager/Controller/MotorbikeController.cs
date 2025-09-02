using Microsoft.AspNetCore.Mvc;
using RentalManager.Models.Entities;
using RentalManager.Services;

namespace RentalManager.Controllers;

[Route("motos")]
[ApiController]
public class MotorbikeController : ControllerBase
{
    const string entityType = "motorbike";
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IRabbitMQRpcService _rabbitMQRpcService;
    private readonly ILogger<MotorbikeController> _logger;
    public MotorbikeController(IRabbitMQService rabbitMQService,
                               IRabbitMQRpcService rabbitMQRpcService,
                               ILogger<MotorbikeController> logger)
    {
        _rabbitMQService = rabbitMQService;
        _rabbitMQRpcService = rabbitMQRpcService;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> CreateMotorbike([FromBody] MotorbikeJson motorbikeJson)
    {
        try
        {
            var motorbike = new Motorbike()
            {
                Id = motorbikeJson.identificador,
                Year = motorbikeJson.ano,
                Model = motorbikeJson.modelo,
                Plate = motorbikeJson.placa,
            };
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "create", entityType);
            _logger.LogInformation($"Creating {motorbike.Id}");
            if (motorbike.Year == 2024)
            {
                try
                {
                    await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "is2024", entityType);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
        return Accepted();
    }
    [HttpGet]
    public async Task<IActionResult> GetAllMotorbikes()
    {
        try
        {
            var mbks = await _rabbitMQRpcService.SendRequestAsync<List<Motorbike>>("get", null, entityType);
            return Ok(mbks);
        }
        catch (Exception ex)
        { 
            _logger.LogError($"{ex.Message}");
            return StatusCode(500, new { Error = ex.Message }); 
        }
    }
    [HttpGet("{placa}")]
    public async Task<IActionResult> GetMotorbikeByPlate(string placa)
    {
        try
        {
            var motorbike = await _rabbitMQRpcService.SendRequestAsync<Motorbike>("getbyplate", placa, entityType);
            if (motorbike is null) 
            {
                _logger.LogInformation($"{placa} not found");
                return NotFound(new { Message = $"{placa} not found" }); 
            }
            return Ok(motorbike);
        }
        catch (Exception ex) 
        {
            _logger.LogError($"{ex.Message}");
            return StatusCode(500, new { Error = ex.Message }); 
        }
    }
    [HttpPut("{id}/placa")]
    public async Task<IActionResult> UpdateMotorbikePlateById([FromRoute] string id, [FromBody] UpdatePlateJson updatePlateJson)
    {
        try
        {
            var motorbike = new Motorbike()
            {
                Id = id,
                Plate = updatePlateJson.placa,
                Year = -1, //gambiarra
                Model = " ", //gambiarra
            };
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "update", entityType);
            return Ok(new { Message = $"Placa modificada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMotorbikeById(string id)
    {
        try
        {
            var motorbike = new Motorbike()
            {
                Id = id,
                Year = -1, //gambiarra
                Model = " ",
                Plate = " ",
            };
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "delete", entityType);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }

}

public class MotorbikeJson {
    public required string identificador { get; set; }
    public required int ano { get; set; }
    public required string modelo { get; set; }
    public required string placa { get; set; }
}

public class UpdatePlateJson {
    public required string placa { get; set; }
}

public class UpdatePlate {
    public required string id { get; set; }
    public required string plate { get; set; }
}
