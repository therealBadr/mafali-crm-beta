using System.Reflection;

namespace MafaliCrm.Web.Services;

public class FieldConflict
{
    public string Field { get; set; } = string.Empty;
    public string MyValue { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
}

public class ApplyResult
{
    public bool Success { get; set; }
    public List<FieldConflict> Conflicts { get; set; } = new();
}

public static class ConflictCheck
{
    public static string FormatForDisplay(object? value) => value switch
    {
        null => "(vide)",
        string s when string.IsNullOrEmpty(s) => "(vide)",
        bool b => b ? "Oui" : "Non",
        _ => value.ToString() ?? "(vide)",
    };

    public static List<FieldConflict> Apply<T>(
        T baseline, T current, T merged, bool force,
        IReadOnlyDictionary<string, string> fieldLabels,
        HashSet<string>? exclude = null)
    {
        exclude ??= new HashSet<string>();
        var conflicts = new List<FieldConflict>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var baselineVal = prop.GetValue(baseline);
            var currentVal = prop.GetValue(current);
            // Untouched by the caller: keep the fresh database value.
            if (Equals(baselineVal, currentVal)) continue;

            var freshVal = prop.GetValue(merged);
            if (!exclude.Contains(prop.Name) && !Equals(baselineVal, freshVal) && !force)
            {
                conflicts.Add(new FieldConflict
                {
                    Field = fieldLabels.TryGetValue(prop.Name, out var label) ? label : prop.Name,
                    MyValue = FormatForDisplay(currentVal),
                    CurrentValue = FormatForDisplay(freshVal),
                });
                continue;
            }
            prop.SetValue(merged, currentVal);
        }
        return conflicts;
    }
}
