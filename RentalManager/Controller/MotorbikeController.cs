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
    public MotorbikeController(IRabbitMQService rabbitMQService,
                               IRabbitMQRpcService rabbitMQRpcService)
    {
        _rabbitMQService = rabbitMQService;
        _rabbitMQRpcService = rabbitMQRpcService;
    }
    [HttpPost]
    public async Task<IActionResult> CreateMotorbike([FromBody] MotorbikeJson motorbikeJson)
    {
        var motorbike = new Motorbike()
        {
            Id = motorbikeJson.identificador,
            Year = motorbikeJson.ano,
            Model = motorbikeJson.modelo,
            Plate = motorbikeJson.placa,
        };
        try
        {
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "create", entityType);
            Console.WriteLine($"Creating {motorbike.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }

        if (motorbike.Year == 2024)
        {
            try
            {
                await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "is2024", entityType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
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
        catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
    }
    // [HttpGet("{id}")]
    // public async Task<IActionResult> GetMotorbikeById(string id)
    // {
    //     try
    //     {
    //         var motorbike = await _rabbitMQRpcService.SendRequestAsync<Motorbike>("getbyid", id, entityType);
    //         if (motorbike is null) { return NotFound(new { Message = $"{id} not found" }); }
    //         return Ok(motorbike);
    //     }
    //     catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
    // }
    [HttpGet("{placa}")]
    public async Task<IActionResult> GetMotorbikeByPlate(string placa)
    {
        try
        {
            var motorbike = await _rabbitMQRpcService.SendRequestAsync<Motorbike>("getbyplate", placa, entityType);
            if (motorbike is null) { return NotFound(new { Message = $"{placa} not found" }); }
            return Ok(motorbike);
        }
        catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
    }
    [HttpPut("{id}/placa")]
    public async Task<IActionResult> UpdateMotorbikePlateById([FromRoute] string id, [FromBody] UpdatePlateJson updatePlateJson)
    {
        var motorbike = new Motorbike()
        {
            Id = id,
            Plate = updatePlateJson.placa,
            Year = -1, //gambiarra
            Model = " ", //gambiarra
        };
        try
        {
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "update", entityType);
            return Ok(new { Message = $"Placa modificada com sucesso" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMotorbikeById(string id)
    {
        var motorbike = new Motorbike()
        {
            Id = id,
            Year = -1, //gambiarra
            Model = " ",
            Plate = " ",
        };
        try
        {
            await _rabbitMQService.PublishMessageAsync<Motorbike>(motorbike, "delete", entityType);
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
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
