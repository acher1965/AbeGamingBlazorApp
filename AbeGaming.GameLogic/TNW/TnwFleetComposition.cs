namespace AbeGaming.GameLogic.TNW
{
    /// <summary>
    /// How many Squadrons of each nation a Fleet has, split into ready and under Refit.
    /// Stored packed in one <see cref="ulong"/> (4 bits per count, at most
    /// <see cref="MaxCount"/>) so the type has value equality - the calculator pages compare
    /// battle definitions with == to decide whether results are still current.
    /// </summary>
    public readonly record struct TnwFleetComposition(ulong Packed)
    {
        public const int MaxCount = 15;
        private const int BitsPerCount = 4;
        private const ulong CountMask = (1UL << BitsPerCount) - 1;

        public static TnwFleetComposition Empty => new(0UL);

        /// <summary>Squadrons of <paramref name="nation"/>, either ready or under Refit.</summary>
        public int Count(TnwNavalNation nation, bool underRefit) =>
            (int)((Packed >> Shift(nation, underRefit)) & CountMask);

        /// <summary>All Squadrons of <paramref name="nation"/>, ready or under Refit.</summary>
        public int Total(TnwNavalNation nation) => Count(nation, false) + Count(nation, true);

        // Dice per packed slot (nation * 2 + refit), in slot order; hot paths avoid enumerating nations.
        private static readonly int[] SlotDice = BuildSlotDice();
        private static readonly int SlotCount = SlotDice.Length;

        private const ulong LowNibbles = 0x0F0F0F0F0F0F0F0FUL;
        private const ulong ByteSumMultiplier = 0x0101010101010101UL;
        private const int TopByteShift = 56;

        /// <summary>
        /// Sum of all packed counts, in constant time: pairs of 4-bit counts are added into
        /// bytes (at most 30 each), then the multiply accumulates every byte into the top byte
        /// (at most 14 x 15 = 210, so it cannot overflow). Hot path of the exact enumeration.
        /// </summary>
        public int TotalSquadrons
        {
            get
            {
                ulong byteSums = (Packed & LowNibbles) + ((Packed >> BitsPerCount) & LowNibbles);
                return (int)((byteSums * ByteSumMultiplier) >> TopByteShift);
            }
        }

        /// <summary>Battle dice the Squadrons roll before any disrupt reductions (13.4).</summary>
        public int Dice
        {
            get
            {
                int dice = 0;
                ulong packed = Packed;
                for (int slot = 0; slot < SlotCount; slot++, packed >>= BitsPerCount)
                    dice += (int)(packed & CountMask) * SlotDice[slot];
                return dice;
            }
        }

        /// <summary>
        /// Battle dice with the "Gallant Danes" event (Admiral Fischer): each Danish Squadron
        /// rolls one extra die (three instead of two, two instead of one under Refit).
        /// </summary>
        public int DiceWithFischer => Dice + Total(TnwNavalNation.Denmark);

        private static int[] BuildSlotDice()
        {
            TnwNavalNation[] nations = Enum.GetValues<TnwNavalNation>();
            int[] slotDice = new int[nations.Length * 2];
            foreach (TnwNavalNation nation in nations)
            {
                slotDice[Shift(nation, false) / BitsPerCount] = nation.SquadronDice(false);
                slotDice[Shift(nation, true) / BitsPerCount] = nation.SquadronDice(true);
            }
            return slotDice;
        }

        /// <summary>A copy with the count for <paramref name="nation"/> set, clamped to 0..<see cref="MaxCount"/>.</summary>
        public TnwFleetComposition With(TnwNavalNation nation, bool underRefit, int count)
        {
            int shift = Shift(nation, underRefit);
            ulong cleared = Packed & ~(CountMask << shift);
            return new TnwFleetComposition(cleared | ((ulong)Math.Clamp(count, 0, MaxCount) << shift));
        }

        /// <summary>
        /// Sets <paramref name="nation"/>'s total Squadrons and how many of them are under Refit
        /// (Refit is clamped to the total).
        /// </summary>
        public TnwFleetComposition WithSquadrons(TnwNavalNation nation, int total, int underRefit)
        {
            int clampedTotal = Math.Clamp(total, 0, MaxCount);
            int clampedRefit = Math.Clamp(underRefit, 0, clampedTotal);
            return With(nation, false, clampedTotal - clampedRefit).With(nation, true, clampedRefit);
        }

        private static int Shift(TnwNavalNation nation, bool underRefit) =>
            ((int)nation * 2 + (underRefit ? 1 : 0)) * BitsPerCount;
    }
}
