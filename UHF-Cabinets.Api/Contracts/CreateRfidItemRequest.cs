using System.ComponentModel.DataAnnotations;

namespace UHFCabinets.Api.Contracts;

public class CreateRfidItemRequest
{
    [Required]
    public string Rfid { get; set; } = string.Empty;

    [Required]
    public string IdProducto { get; set; } = string.Empty;
}
