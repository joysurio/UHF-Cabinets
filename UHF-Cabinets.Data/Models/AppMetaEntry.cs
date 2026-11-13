using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UHFCabinets.Data.Models;

[Table("app_meta")]
public class AppMetaEntry
{
    [Key]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
