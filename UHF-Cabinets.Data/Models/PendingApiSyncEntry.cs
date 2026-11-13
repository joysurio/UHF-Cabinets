using System.ComponentModel.DataAnnotations.Schema;

namespace UHFCabinets.Data.Models;

[Table("pending_api_sync")]
public class PendingApiSyncEntry
{
    public int Id { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}
