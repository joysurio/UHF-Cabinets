using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UHFCabinets.Data.Models;

[Table("items")]
public class CabinetItem
{
    [Key]
    public string Rfid { get; set; } = string.Empty;

    [Required]
    public string Producto { get; set; } = string.Empty;
}
