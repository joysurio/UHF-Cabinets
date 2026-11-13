namespace UHFCabinets.Services;

public static class OperationContext
{
    public static string? IdMov { get; set; }
    public static HashSet<string> BeforeSnapshot { get; } = new(StringComparer.Ordinal);
    public static HashSet<string> AfterSnapshot { get; } = new(StringComparer.Ordinal);
    public static List<string> Removed { get; } = new();
    public static List<string> Added { get; } = new();
    public static List<string> UnknownRfidsPending { get; } = new();
    public static int SummarySalidas { get; set; }
    public static int SummaryEntradas { get; set; }

    public static void ResetForNewOperation()
    {
        IdMov = null;
        BeforeSnapshot.Clear();
        AfterSnapshot.Clear();
        Removed.Clear();
        Added.Clear();
        UnknownRfidsPending.Clear();
        SummarySalidas = 0;
        SummaryEntradas = 0;
    }

    public static void ComputeDiffFromSnapshots()
    {
        Removed.Clear();
        Added.Clear();
        Removed.AddRange(BeforeSnapshot.Except(AfterSnapshot).Distinct(StringComparer.Ordinal));
        Added.AddRange(AfterSnapshot.Except(BeforeSnapshot).Distinct(StringComparer.Ordinal));
    }

    public static void RebuildUnknownList(Func<string, bool> hasItemRow)
    {
        UnknownRfidsPending.Clear();
        foreach (var tag in AfterSnapshot.Distinct(StringComparer.Ordinal))
        {
            if (!hasItemRow(tag))
                UnknownRfidsPending.Add(tag);
        }
    }
}
