namespace MafaliCrm.Web.Services;

public static class GridLayout
{
    public static string WrapperStyle(int totalWidth) => $"width: 100%; min-width: {totalWidth}px;";

    public static string ColStyle(int width, bool isLast) =>
        isLast ? $"flex: 1 1 {width}px;" : $"flex: 0 0 {width}px;";
}
