using System.Collections;

namespace AbeGaming.GameLogic
{
    /// <summary>
    /// An immutable list with value (sequence) equality, for use inside records. The calculator
    /// pages compare battle definitions with == to decide whether results are still current, and
    /// a plain list or array in a record would only compare references.
    /// </summary>
    public sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
    {
        private readonly T[] _items;

        public EquatableList(IEnumerable<T> items) => _items = items.ToArray();

        public static EquatableList<T> Empty { get; } = new([]);

        public int Count => _items.Length;

        public T this[int index] => _items[index];

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public bool Equals(EquatableList<T>? other) =>
            other is not null && _items.AsSpan().SequenceEqual(other._items, EqualityComparer<T>.Default);

        public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

        public override int GetHashCode()
        {
            HashCode hash = new();
            foreach (T item in _items)
                hash.Add(item);
            return hash.ToHashCode();
        }
    }
}
