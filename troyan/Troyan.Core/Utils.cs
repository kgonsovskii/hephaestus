namespace Troyan.Core;

public static class Utils
{
    public static string[] SortWithPriority(
        this IEnumerable<string> items,
        string[] priorityItems,
        string[]? deprioritizedItems = null)
    {
        var prioritySet = new HashSet<string>(priorityItems);
        var deprioritizedSet = deprioritizedItems != null
            ? new HashSet<string>(deprioritizedItems)
            : new HashSet<string>();

        return items
            .OrderBy(item => !prioritySet.Contains(item))
            .ThenBy(item => prioritySet.Contains(item) ? Array.IndexOf(priorityItems, item) : int.MaxValue)
            .ThenBy(item => deprioritizedSet.Contains(item))
            .ToArray();
    }

    public static string[] Exclude(this IEnumerable<string> items, IEnumerable<string> itemsToExclude)
    {
        return items.Where(item => !itemsToExclude.Contains(item.Trim())).ToArray();
    }
}
