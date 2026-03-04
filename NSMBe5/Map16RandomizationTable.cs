using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace NSMBe5
{
    internal static class Map16RandomizationTable
    {
        private const string RelativeJsonPath = "Data/Map16RandomizationTable.json";
        private static readonly object LoadLock = new object();
        private static Dictionary<int, int[]> randomizationRangesByTileset;

        // Mapping is loaded from Data/Map16RandomizationTable.json and falls back
        // to this in-code copy if the file is missing or invalid.
        private static Dictionary<int, int[]> RandomizationRangesByTileset
        {
            get
            {
                if (randomizationRangesByTileset != null)
                    return randomizationRangesByTileset;

                lock (LoadLock)
                {
                    if (randomizationRangesByTileset == null)
                    {
                        randomizationRangesByTileset = LoadFromJson();
                        if (randomizationRangesByTileset == null)
                            randomizationRangesByTileset = GetFallbackRanges();
                    }
                }

                return randomizationRangesByTileset;
            }
        }

        public static HashSet<int> GetRandomizedTiles(ushort tilesetId, int tileCount)
        {
            HashSet<int> result = new HashSet<int>();

            if (!RandomizationRangesByTileset.TryGetValue(tilesetId, out int[] ranges))
                return result;

            for (int i = 0; i + 1 < ranges.Length; i += 2)
                AddRange(result, ranges[i], ranges[i + 1], tileCount);

            return result;
        }

        private static Dictionary<int, int[]> LoadFromJson()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RelativeJsonPath);
                if (!File.Exists(jsonPath))
                    return null;

                string json = File.ReadAllText(jsonPath);
                Dictionary<string, int[]> parsed = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json);
                if (parsed == null || parsed.Count == 0)
                    return null;

                Dictionary<int, int[]> loaded = new Dictionary<int, int[]>();
                foreach (KeyValuePair<string, int[]> entry in parsed)
                {
                    if (!int.TryParse(entry.Key, out int tilesetId))
                        continue;

                    int[] ranges = entry.Value;
                    if (ranges == null || ranges.Length < 2)
                        continue;

                    int pairLength = ranges.Length - (ranges.Length % 2);
                    if (pairLength <= 0)
                        continue;

                    int[] normalized = new int[pairLength];
                    Array.Copy(ranges, normalized, pairLength);
                    loaded[tilesetId] = normalized;
                }

                if (loaded.Count == 0)
                    return null;

                return loaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load map16 randomization table JSON: " + ex.Message);
                return null;
            }
        }

        private static Dictionary<int, int[]> GetFallbackRanges()
        {
            return new Dictionary<int, int[]>
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
