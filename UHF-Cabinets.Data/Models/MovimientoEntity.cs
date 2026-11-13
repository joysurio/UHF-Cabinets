using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UHFCabinets.Data.Models;

[Table("movimientos")]
public class MovimientoEntity
{
    public int Id { get; set; }

    [Required]
    public string IdMov { get; set; } = string.Empty;

    [Required]
    public string Rfid { get; set; } = string.Empty;

    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string TipoOperacion { get; set; } = string.Empty;

    [Required]
    public string Fecha { get; set; } = string.Empty;
}
