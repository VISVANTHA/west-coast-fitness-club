using Newtonsoft.Json;

namespace WestCoastFitness.Infrastructure;

/// <summary>Formats structured audit lines. Uses Newtonsoft.Json (deliberately pinned to a vulnerable version).</summary>
public static class AuditLog
{
    public static string Format(string action, object? details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return JsonConvert.SerializeObject(new { action, details }, Formatting.None);
    }
}
