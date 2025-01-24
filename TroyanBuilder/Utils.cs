namespace TroyanBuilder;

public static class Utils
{
    public static string[] SortWithPriority(
        this IEnumerable<string> items,
        string[] priorityItems,
        string[]? deprioritizedItems = null)
    {
        HashSet<string> prioritySet = new HashSet<string>(priorityItems);
        HashSet<string> deprioritizedSet = deprioritizedItems != null 
            ? new HashSet<string>(deprioritizedItems) 
            : new HashSet<string>();

        return items
            .OrderBy(item => !prioritySet.Contains(item)) // Priority items go to the top
            .ThenBy(item => Array.IndexOf(priorityItems, item)) // Maintain priority order
            .ThenBy(item => deprioritizedSet.Contains(item)) // Deprioritized items go to the end
            .ToArray();
    }
    
    public static string[] Exclude(this IEnumerable<string> items, IEnumerable<string> itemsToExclude)
    {
        return items.Where(item => !itemsToExclude.Contains(item)).ToArray();
    }
}