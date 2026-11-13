using System.ComponentModel.DataAnnotations;

namespace UHFCabinets.Api.Contracts;

public class MovementRequest
{
    [Required]
    public string IdMov { get; set; } = string.Empty;

    [Required]
    public string Rfid { get; set; } = string.Empty;

    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string TipoOperacion { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset? Fecha { get; set; }

    [Required]
    public string CabinetId { get; set; } = string.Empty;
}

public class BulkMovementsRequest
{
    [Required]
    [MinLength(1)]
    public List<MovementRequest> Items { get; set; } = new();
}
