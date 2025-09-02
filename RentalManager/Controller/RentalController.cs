using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Models.Entities;
using RentalManager.Services;
using RentalManager.Utils;

namespace RentalManager.Controllers;

[ApiController]
[Route("locacao")]
public class RentalController : ControllerBase
{
    const string entityType = "rental";
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IRabbitMQRpcService _rabbitMQRpcService;
    private readonly ILogger<RentalController> _logger;
    public RentalController(IRabbitMQService rabbitMQService,
                            IRabbitMQRpcService rabbitMQRpcService,
                            ILogger<RentalController> logger)
    {
        _rabbitMQService = rabbitMQService;
        _rabbitMQRpcService = rabbitMQRpcService;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> CreateRental([FromBody] RentalJson rentalJson)
    {
        try
        {
            var startDate = DateTime.TryParse(rentalJson.data_inicio, out var sd) ? sd : DateTime.Now.AddDays(1);
            var endDate = DateTime.TryParse(rentalJson.data_termino, out var ed) ? ed : startDate.AddDays((int)rentalJson.plano);
            var rental = new Rental()
            {
                Id = rentalJson.identificador, //check if it is really passed in the json since it is not in the example
                DeliveryManId = rentalJson.entregador_id,
                MotorbikeId = rentalJson.moto_id,
                StartDate = startDate,
                EndDate = endDate,
                ExpectedEndDate = DateTime.TryParse(rentalJson.data_previsao_termino, out var eed) ? eed : endDate,
                RentalType = ToRentalType(rentalJson.plano),
            };
            await _rabbitMQService.PublishMessageAsync<Rental>(rental, "create", entityType);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRentalById([FromRoute] string id)
    {
        try
        {
            var rental = await _rabbitMQRpcService.SendRequestAsync<Rental>("getbyid", id, entityType);
            if (rental is null)
            {
                _logger.LogInformation($"Rental not found by id {id}");
                return NotFound(new { Message = $"Locação não encontrada" });
            }
            var rentalShown = new RentalJsonShown(rental);
            rentalShown.valor_diaria = RentalTypePriceToShow(rental.RentalType);
            return Ok(rentalShown);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Locação não encontrada" });
        }
    }
    [HttpPut("{id}/devolucao")]
    public async Task<IActionResult> UpdateRentalExpectedEndDate([FromRoute] string id, [FromBody] UpdateEEDJson updateEEDJson)
    {
        var rental = new Rental()
        {
            Id = id,
            ExpectedEndDate = DateTime.TryParse(updateEEDJson.data_devolucao, out var eed) ? eed : DateTime.Now,

            DeliveryManId = " ", //gambiarra
            MotorbikeId = " ",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now,
            RentalType = RentalType.unknown,
        };
        try
        {
            var r = await _rabbitMQRpcService.SendRequestAsync<RentalResponse>("update", rental, entityType);
            if (r == null)
            {
                throw new Exception();
            }
            return Ok(new { Message = $"Data de devolução informada com sucesso. Valor total do aluguel R${r.TotalRentalValue},00" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }
    private RentalType ToRentalType(int? i)
    {
        return i switch
        {
            7 => RentalType.Days7,
            15 => RentalType.Days15,
            30 => RentalType.Days30,
            45 => RentalType.Days45,
            50 => RentalType.Days50,
            _ => RentalType.unknown,
        };
    }
    private int RentalTypePriceToShow(RentalType? t) {
        return t switch
        {
            RentalType.unknown => (int)RentalTypePrices.unknown,
            RentalType.Days7 => (int)RentalTypePrices.Days7,
            RentalType.Days15 => (int)RentalTypePrices.Days15,
            RentalType.Days30 => (int)RentalTypePrices.Days30,
            RentalType.Days45 => (int)RentalTypePrices.Days45,
            RentalType.Days50 => (int)RentalTypePrices.Days50,
            _ => (int)RentalTypePrices.unknown,
        };
    }
}

public class RentalJson_ {
    public string? identificador { get; set; }
    public string? entregador_id { get; set; }
    public string? moto_id {get; set;}
    public string? data_inicio {get; set;}
    public string? data_termino {get; set;}
    public string? data_previsao_termino {get; set;}
}

public class RentalJson : RentalJson_
{
    public required int plano { get; set; }
}

public class RentalJsonShown : RentalJson_
{
    [SetsRequiredMembers]
    public RentalJsonShown(Rental r)
    {
        try
        {
            identificador = r.Id;
            entregador_id = r.DeliveryManId;
            moto_id = r.MotorbikeId;
            data_inicio = r.StartDate.ToString();
            data_termino = r.EndDate.ToString();
            data_previsao_termino = r.ExpectedEndDate.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
        }
    }
    public required int? valor_diaria { get; set; } 
}

public class UpdateEEDJson
{
    public required string data_devolucao { get; set; }
}

public class RentalResponse : Rental
{
    public double? TotalRentalValue { get; set; }
}
