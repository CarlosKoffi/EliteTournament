namespace CPElite.Web.Services;

public static class EaArchetypeMapper
{
    private static readonly IReadOnlyDictionary<int, string> Names = new Dictionary<int, string>
    {
        [1] = "Shot Stopper",
        [2] = "Sweeper Keeper",
        [3] = "Progressor",
        [4] = "Boss",
        [5] = "Engine",
        [6] = "Marauder",
        [7] = "Recycler",
        [8] = "Maestro",
        [9] = "Creator",
        [10] = "Spark",
        [11] = "Magician",
        [12] = "Finisher",
        [13] = "Target"
    };

    public static string Label(int? mapId)
    {
        if (!mapId.HasValue)
        {
            return "-";
        }

        return Names.TryGetValue(mapId.Value, out var name)
            ? name
            : $"Archetype {mapId.Value}";
    }

    public static string Label(int? mapId, string? fallback)
    {
        var mapped = Label(mapId);
        if (mapped != "-")
        {
            return mapped;
        }

        return string.IsNullOrWhiteSpace(fallback) ? "-" : fallback;
    }
}
