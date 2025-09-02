using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Models.Entities;
using RentalManager.Services;
using RentalManager.Utils;

namespace RentalManager.Controllers;

[Route("entregadores")]
[ApiController]
public class DeliveryManController : ControllerBase
{
    const string entityType = "delivery-man";
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<DeliveryManController> _logger;
    public DeliveryManController(IRabbitMQService rabbitMQService, ILogger<DeliveryManController> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeliveryMan([FromBody] DeliveryManJson deliveryManJson)
    {
        try
        {
            DateTime birthDate = new();
            if (DateTime.TryParse(deliveryManJson.data_nascimento, out var bd))
            {
                birthDate = bd;
            }
            else
            {
                _logger.LogInformation("Couldn't parse birth date received");
                return BadRequest(new { Message = $"Dados inválidos" });
            }
            var deliveryMan = new DeliveryMan()
            {
                Id = deliveryManJson.identificador,
                Name = deliveryManJson.nome,
                LegalId = deliveryManJson.cnpj,
                BirthDate = birthDate,
                DriversLicense = deliveryManJson.numero_cnh,
                DriversLicenseType = ToDriversLicenseType(deliveryManJson.tipo_cnh),
                DriversLicensePictureLocal = deliveryManJson.imagem_cnh,
            };
            await _rabbitMQService.PublishMessageAsync<DeliveryMan>(deliveryMan, "create", entityType);
            _logger.LogInformation("Creating new deliveryMan");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}");
            return BadRequest(new { Message = $"Dados inválidos" });
        }
    }
    private DriversLicenseType ToDriversLicenseType(string? driversLicense)
    {
        return driversLicense switch
        {
            "A" => DriversLicenseType.A,
            "B" => DriversLicenseType.B,
            "AB" => DriversLicenseType.AB,
            _ => DriversLicenseType.unknown,
        };
    }
}

public class DeliveryManJson {
    public required string identificador { get; set; }
    public required string nome { get; set; }
    public required string cnpj { get; set; }
    public required string data_nascimento { get; set; }
    public required string numero_cnh { get; set; }
    public required string tipo_cnh { get; set; }
    public required string imagem_cnh { get; set; }
};

