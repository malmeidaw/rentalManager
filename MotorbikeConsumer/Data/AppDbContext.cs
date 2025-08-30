using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MotorbikeConsumer.Models.Entities;

namespace MotorbikeConsumer.Data;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppDbContext> _logger;
    public AppDbContext(DbContextOptions options, IConfiguration configuration,
                        ILogger<AppDbContext> logger) : base(options)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public DbSet<Motorbike> Motorbikes { get; set; }
    public DbSet<DeliveryMan> DeliveryMans { get; set;}
    public DbSet<Rental> Rentals { get; set;}

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? connectionString = null;
        if (!optionsBuilder.IsConfigured) connectionString = _configuration?.GetConnectionString("DbConnection");
        if (!string.IsNullOrEmpty(connectionString)) optionsBuilder.UseNpgsql(connectionString);
        else _logger.LogError("Couldn't load connectionString");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rental_manager");
        modelBuilder.Entity<Motorbike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Plate).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Model).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Year).IsRequired();
                entity.HasIndex(e => e.Plate).IsUnique(); //"A placa é um dado único e não pode se repetir."
                entity.Property(e => e.Id).ValueGeneratedOnAdd();//check if this is correct
            }
        );
        modelBuilder.Entity<DeliveryMan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.LegalId).IsRequired();//.HasMaxLength(n); //check maximum size of cnpj
                entity.HasIndex(e => e.LegalId).IsUnique(); //O cnpj é único e não pode se repetir
                entity.Property(e => e.DriversLicense).IsRequired();
                entity.HasIndex(e => e.DriversLicense).IsUnique(); // O número da CNH é único e não pode se repetir.
                entity.Property(e => e.DriversLicenseType).IsRequired();
                entity.Property(e => e.BirthDate)
                    .IsRequired()
                    .HasColumnName("birth_date")
                    .HasConversion(
                        _ => _.Kind == DateTimeKind.Unspecified ?
                            DateTime.SpecifyKind(_, DateTimeKind.Utc) :
                            _.ToUniversalTime(),
                        _ => DateTime.SpecifyKind(_, DateTimeKind.Utc)
                    );
            }
        );
        modelBuilder.Entity<Rental>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(r => r.DeliveryMan)
                    .WithMany()
                    .HasForeignKey(r => r.DeliveryManId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Motorbike)
                    .WithMany()
                    .HasForeignKey(r => r.MotorbikeId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.Property(e => e.RentalType)
                    .IsRequired()
                    .HasConversion<string>();
                
                entity.Property(e => e.StartDate)
                    .IsRequired()
                    .HasColumnName("start_date")
                    .HasConversion(
                        _ => _.Kind == DateTimeKind.Unspecified ?
                            DateTime.SpecifyKind(_, DateTimeKind.Utc) :
                            _.ToUniversalTime(),
                        _ => DateTime.SpecifyKind(_, DateTimeKind.Utc)
                    );
                
                entity.Property(e => e.EndDate)
                    .IsRequired()
                    .HasColumnName("end_date")
                    .HasConversion(
                        _ => _.Kind == DateTimeKind.Unspecified ?
                            DateTime.SpecifyKind(_, DateTimeKind.Utc) :
                            _.ToUniversalTime(),
                        _ => DateTime.SpecifyKind(_, DateTimeKind.Utc)
                    );
                
                entity.Property(e => e.ExpectedEndDate)
                    .IsRequired()
                    .HasColumnName("expected_end_date")
                    .HasConversion(
                        _ => _.Kind == DateTimeKind.Unspecified ?
                            DateTime.SpecifyKind(_, DateTimeKind.Utc) :
                            _.ToUniversalTime(),
                        _ => DateTime.SpecifyKind(_, DateTimeKind.Utc)
                    );
            }
        );
    }
}