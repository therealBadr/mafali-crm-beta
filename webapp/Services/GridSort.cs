namespace MafaliCrm.Web.Services;

public class SortState
{
    public string? Column { get; private set; }
    public bool Descending { get; private set; }

    public void Toggle(string column)
    {
        if (Column != column)
        {
            Column = column;
            Descending = false;
        }
        else if (!Descending)
        {
            Descending = true;
        }
        else
        {
            Column = null;
            Descending = false;
        }
    }

    public string ArrowFor(string column) =>
        Column != column ? "" : Descending ? " ▼" : " ▲";

    public string CssClass(string column) =>
        Column == column ? "sortable sorted" : "sortable";
}

public static class GridSort
{
    public static List<T> Apply<T>(List<T> items, SortState state, Func<T, string, string> getText)
    {
        if (state.Column is null) return items;
        var column = state.Column;
        var ordered = items.OrderBy(item => getText(item, column), Comparer<string>.Create(CompareCells));
        return (state.Descending ? ordered.Reverse() : (IEnumerable<T>)ordered).ToList();
    }

    private static int CompareCells(string? a, string? b)
    {
        a ??= string.Empty;
        b ??= string.Empty;

        if (long.TryParse(a, out var na) && long.TryParse(b, out var nb))
            return na.CompareTo(nb);

        if (DateOnly.TryParseExact(a, "dd/MM/yyyy", out var da) && DateOnly.TryParseExact(b, "dd/MM/yyyy", out var db))
            return da.CompareTo(db);

        if (TimeOnly.TryParseExact(a, "HH:mm", out var ta) && TimeOnly.TryParseExact(b, "HH:mm", out var tb))
            return ta.CompareTo(tb);

        if (DateTime.TryParseExact(a, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dta) &&
            DateTime.TryParseExact(b, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dtb))
            return dta.CompareTo(dtb);

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
