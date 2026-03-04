using System.Collections.Generic;

namespace NSMBe5
{
    internal static class Map16RandomizationTable
    {
        // Mapping sourced from nsmbcentral
        // Entries are encoded as start/end pairs (inclusive).
        private static readonly Dictionary<int, int[]> RandomizationRangesByTileset = new Dictionary<int, int[]>
        {
            { 0,  new[] { 0, 5 } },
            { 2,  new[] { 0, 5 } },
            { 3,  new[] { 0, 5, 48, 53 } },
            { 5,  new[] { 0, 5 } },
            { 6,  new[] { 0, 5 } },
            { 8,  new[] { 0, 5 } },
            { 9,  new[] { 0, 5 } },
            { 10, new[] { 0, 5 } },
            { 11, new[] { 0, 2 } },
            { 12, new[] { 0, 5 } },
            { 13, new[] { 0, 5 } },
            { 14, new[] { 0, 5 } },
            { 15, new[] { 0, 5 } },
            { 16, new[] { 0, 5 } },
            { 17, new[] { 96, 99 } },
            { 19, new[] { 0, 2 } },
            { 20, new[] { 0, 5 } },
            { 24, new[] { 0, 3 } },
            { 25, new[] { 0, 3 } },
            { 26, new[] { 96, 99 } },
            { 27, new[] { 0, 5 } },
            { 28, new[] { 96, 99 } },
            { 31, new[] { 0, 5 } },
            { 33, new[] { 0, 5 } },
            { 34, new[] { 0, 5 } },
            { 35, new[] { 0, 5, 48, 53 } },
            { 36, new[] { 0, 5, 48, 53 } },
            { 37, new[] { 0, 5 } },
            { 38, new[] { 0, 5 } },
            { 40, new[] { 0, 5 } },
            { 50, new[] { 0, 5 } },
            { 51, new[] { 0, 2 } },
            { 52, new[] { 0, 5 } },
            { 53, new[] { 0, 5 } },
            { 54, new[] { 0, 2 } },
            { 56, new[] { 0, 5 } },
            { 57, new[] { 0, 5 } },
            { 60, new[] { 0, 5 } },
            { 64, new[] { 0, 5 } },
            { 66, new[] { 0, 5 } },
            { 73, new[] { 0, 5, 48, 53 } },
            { 75, new[] { 0, 5 } }
        };

        public static HashSet<int> GetRandomizedTiles(ushort tilesetId, int tileCount)
        {
            HashSet<int> result = new HashSet<int>();

            if (!RandomizationRangesByTileset.TryGetValue(tilesetId, out int[] ranges))
                return result;

            for (int i = 0; i + 1 < ranges.Length; i += 2)
                AddRange(result, ranges[i], ranges[i + 1], tileCount);

            return result;
        }

        private static void AddRange(HashSet<int> output, int startInclusive, int endInclusive, int tileCount)
        {
            int start = startInclusive;
            int end = endInclusive;

            if (end < start)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            if (tileCount > 0)
            {
                if (start >= tileCount)
                    return;

                if (end >= tileCount)
                    end = tileCount - 1;
            }

            if (end < 0)
                return;

            if (start < 0)
                start = 0;

            for (int value = start; value <= end; value++)
                output.Add(value);
        }
    }
}
