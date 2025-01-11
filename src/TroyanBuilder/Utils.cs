namespace TroyanBuilder;

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
            .OrderBy(item => !prioritySet.Contains(item)) // Prioritize items in the priority set
            .ThenBy(item => prioritySet.Contains(item) ? Array.IndexOf(priorityItems, item) : int.MaxValue) // Maintain priority order
            .ThenBy(item => deprioritizedSet.Contains(item)) // Push deprioritized items to the end
            .ToArray();
    }
    
    public static string[] Exclude(this IEnumerable<string> items, IEnumerable<string> itemsToExclude)
    {
        return items.Where(item => !itemsToExclude.Contains(item.Trim())).ToArray();
    }
}