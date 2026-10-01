using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbeGaming.GameLogic.PoG
{
    /// <summary>
    /// The unit types available in the detailed calculator mode, loaded once from the embedded
    /// resource PoGUnitTypes.json.
    /// </summary>
    public static class PoGUnitCatalog
    {
        private const string ResourceName = "AbeGaming.GameLogic.PoG.PoGUnitTypes.json";

        private static readonly Lazy<IReadOnlyDictionary<string, PoGUnitType>> Types = new(Load);

        /// <summary>Every unit type, in the order of the configuration file.</summary>
        public static IReadOnlyList<PoGUnitType> All => [.. Types.Value.Values];

        /// <summary>The unit type with this id.</summary>
        /// <exception cref="KeyNotFoundException">No unit type has this id.</exception>
        public static PoGUnitType Get(string id) =>
            Types.Value.TryGetValue(id, out PoGUnitType? type)
                ? type
                : throw new KeyNotFoundException($"Unknown PoG unit type '{id}'.");

        private static IReadOnlyDictionary<string, PoGUnitType> Load()
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

            Dictionary<string, PoGUnitType> types = new(StringComparer.Ordinal);
            foreach (PoGUnitType type in file.Units)
            {
                if (!types.TryAdd(type.Id, type))
                    throw new InvalidOperationException($"Duplicate PoG unit type '{type.Id}'.");
            }

            foreach (PoGUnitType type in types.Values)
            {
                bool isArmy = type.Kind == PoGUnitKind.Army;
                if (isArmy != (type.ReplacementCorps is not null))
                    throw new InvalidOperationException($"PoG unit type '{type.Id}': only Armies have a replacement Corps.");
                if (type.ReplacementCorps is not null
                    && (!types.TryGetValue(type.ReplacementCorps, out PoGUnitType? corps) || corps.Kind != PoGUnitKind.Corps))
                    throw new InvalidOperationException($"PoG unit type '{type.Id}': replacement '{type.ReplacementCorps}' is not a Corps.");
            }

            return types;
        }

        private sealed record CatalogFile(List<PoGUnitType> Units);
    }
}
