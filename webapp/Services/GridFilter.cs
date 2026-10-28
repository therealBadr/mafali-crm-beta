namespace MafaliCrm.Web.Services;

public class GridFilterState
{
    private readonly Dictionary<string, string> _queries = new();
    private readonly Dictionary<string, string> _labels = new();
    private string? OpenColumn;

    public bool IsOpen(string column) => OpenColumn == column;

    public bool HasAny => _queries.Count > 0;

    public int ActiveCount => _queries.Count;

    public IReadOnlyDictionary<string, string> ActiveFilters => _queries;

    public IEnumerable<(string Column, string Label, string Query)> ActiveChips =>
        _queries.Select(kv => (kv.Key, _labels.TryGetValue(kv.Key, out var l) ? l : kv.Key, kv.Value));

    public string QueryFor(string column) => _queries.TryGetValue(column, out var q) ? q : "";

    public string CssClass(string column) =>
        _queries.ContainsKey(column) ? "filterable filtered" : "filterable";

    public void ToggleOpen(string column) => OpenColumn = OpenColumn == column ? null : column;

    public void SetQuery(string column, string text, string label)
    {
        if (text.Length > 0)
        {
            _queries[column] = text;
            _labels[column] = label;
        }
        else
        {
            _queries.Remove(column);
            _labels.Remove(column);
        }
    }

    public void Cancel(string column)
    {
        OpenColumn = null;
        _queries.Remove(column);
        _labels.Remove(column);
    }

    public void RemoveColumn(string column)
    {
        _queries.Remove(column);
        _labels.Remove(column);
        if (OpenColumn == column) OpenColumn = null;
    }

    public void ClearAll()
    {
        _queries.Clear();
        _labels.Clear();
        OpenColumn = null;
    }
}

public static class GridFilter
{
    public static List<T> Apply<T>(List<T> items, GridFilterState state, Func<T, string, string> getText)
    {
        if (!state.HasAny) return items;
        var active = state.ActiveFilters;
        return items.Where(item =>
            active.All(kv => getText(item, kv.Key).Contains(kv.Value, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }
}
