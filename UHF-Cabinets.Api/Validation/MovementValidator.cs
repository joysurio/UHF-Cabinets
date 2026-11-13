using UHFCabinets.Api.Contracts;

namespace UHFCabinets.Api.Validation;

public static class MovementValidator
{
    private static readonly HashSet<string> ValidTipos = new(StringComparer.OrdinalIgnoreCase)
    {
        "ENTRADA",
        "SALIDA"
    };

    public static string? Validate(MovementRequest m)
    {
        if (string.IsNullOrWhiteSpace(m.IdMov)) return "idMov is required.";
        if (string.IsNullOrWhiteSpace(m.Rfid)) return "rfid is required.";
        if (string.IsNullOrWhiteSpace(m.Usuario)) return "usuario is required.";
        if (string.IsNullOrWhiteSpace(m.TipoOperacion)) return "tipoOperacion is required.";
        if (!ValidTipos.Contains(m.TipoOperacion.Trim()))
            return "tipoOperacion must be ENTRADA or SALIDA.";
        if (m.Fecha == null) return "fecha is required.";
        if (m.Fecha.Value == default) return "fecha is invalid.";
        if (string.IsNullOrWhiteSpace(m.CabinetId)) return "cabinetId is required.";
        return null;
    }

    public static string NormalizeTipo(string tipo) => tipo.Trim().ToUpperInvariant();
}
