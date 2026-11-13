namespace UHFCabinets.Api.Data.Entities;

public class CabinetRegistration
{
    public int Id { get; set; }
    public string CabinetId { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
