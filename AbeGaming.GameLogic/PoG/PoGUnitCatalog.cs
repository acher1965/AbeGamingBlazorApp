using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// The unit types and factions available in the detailed calculator mode, loaded once from
    /// the embedded resource PoGUnitTypes.json.
    /// </summary>
    public static class PoGUnitCatalog
    {
        private const string ResourceName = "AbeGaming.GameLogic.PoG.PoGUnitTypes.json";

        private static readonly Lazy<Catalog> Data = new(Load);

        /// <summary>Every unit type, in the order of the configuration file (the display order).</summary>
        public static IReadOnlyList<PoGUnitType> All => Data.Value.Ordered;

        /// <summary>The unit types of one faction, in display order.</summary>
        public static IReadOnlyList<PoGUnitType> ForFaction(PoGFaction faction) =>
            [.. Data.Value.Ordered.Where(t => t.Faction == faction)];

        /// <summary>The display settings of a faction.</summary>
        public static PoGFactionInfo Faction(PoGFaction faction) => Data.Value.Factions[faction];

        /// <summary>The unit type with this id.</summary>
        /// <exception cref="KeyNotFoundException">No unit type has this id.</exception>
        public static PoGUnitType Get(string id) =>
            Data.Value.ById.TryGetValue(id, out PoGUnitType? type)
                ? type
                : throw new KeyNotFoundException($"Unknown PoG unit type '{id}'.");

        private static Catalog Load()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
            CatalogFile file = JsonSerializer.Deserialize<CatalogFile>(stream, options)
                ?? throw new InvalidOperationException($"'{ResourceName}' is empty.");

            Dictionary<string, PoGUnitType> byId = new(StringComparer.Ordinal);
            foreach (PoGUnitType type in file.Units)
            {
                if (!byId.TryAdd(type.Id, type))
                    throw new InvalidOperationException($"Duplicate PoG unit type '{type.Id}'.");
            }

            foreach (PoGUnitType type in file.Units)
            {
                bool isArmy = type.Kind == PoGUnitKind.Army;
                if (isArmy != (type.ReplacementCorps is not null))
                    throw new InvalidOperationException($"PoG unit type '{type.Id}': only Armies have a replacement Corps.");
                if (type.ReplacementCorps is not null
                    && (!byId.TryGetValue(type.ReplacementCorps, out PoGUnitType? corps) || corps.Kind != PoGUnitKind.Corps || corps.Faction != type.Faction))
                    throw new InvalidOperationException($"PoG unit type '{type.Id}': replacement '{type.ReplacementCorps}' is not a Corps of the same faction.");
                if (type.AttackerLossPriority is <= 0)
                    throw new InvalidOperationException($"PoG unit type '{type.Id}': attackerLossPriority must be positive.");
            }

            Dictionary<PoGFaction, PoGFactionInfo> factions = [];
            foreach (PoGFactionInfo faction in file.Factions)
            {
                if (!factions.TryAdd(faction.Faction, faction))
                    throw new InvalidOperationException($"Duplicate PoG faction '{faction.Faction}'.");
                if (!byId.TryGetValue(faction.DefaultUnit, out PoGUnitType? unit) || unit.Faction != faction.Faction)
                    throw new InvalidOperationException($"PoG faction '{faction.Faction}': default unit '{faction.DefaultUnit}' is not one of its units.");
            }
            foreach (PoGFaction faction in Enum.GetValues<PoGFaction>())
            {
                if (!factions.ContainsKey(faction))
                    throw new InvalidOperationException($"PoG faction '{faction}' is missing from '{ResourceName}'.");
            }

            return new Catalog(file.Units, byId, factions);
        }

        private sealed record CatalogFile(List<PoGFactionInfo> Factions, List<PoGUnitType> Units);

        private sealed record Catalog(
            IReadOnlyList<PoGUnitType> Ordered,
            IReadOnlyDictionary<string, PoGUnitType> ById,
            IReadOnlyDictionary<PoGFaction, PoGFactionInfo> Factions);
    }
}
