using UnityEngine;
using System.Collections.Generic;
using static Tile;
using System.Linq;
using System.Collections;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    private Dictionary<Vector2Int, Tile> gridTiles = new Dictionary<Vector2Int, Tile>();
    public HashSet<Vector2Int> reachableMoveTiles = new HashSet<Vector2Int>();

    public GameObject tilePrefab;
    public int width = 10, height = 10;

    public void GenerateGrid()
    {
        ClearGrid();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                    tile.Init(x, y);
            }
        }
    }

    void ClearGrid()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    private void Awake()
    {
        Instance = this;
        CacheTiles();
    }

    private void Start()
    {
        //CacheTiles();
        GenerateGrid();
        Camera.main.GetComponent<CameraController>().CenterOnGrid();
        //RandomizeTiles();

        // WFC 1
        // Generate seed > Generate WFC Map > Apply to tiles
        int[,] seed = GenerateRandomSeed(4, 4);
        DebugMap(seed, "seed");
        int[,] mapData = WFCGenerator.Generate(seed, 10, 10);
        DebugMap(mapData, "WFC Map");
        ApplyMapToTiles(mapData);

        // Wait 5 seconds and then do the folloings
        Debug.Log("Waiting 5 seconds before generating WFC Map 2...");
        StartCoroutine(WaitAndGenerateWFCMap2(seed));
    }

    IEnumerator WaitAndGenerateWFCMap2(int[,] seed)
    {
        //yield return new WaitForSeconds(5f);// WFC 2
        //// 1. Generate a random seed (for example, a 4x4 pattern with values 0 to 2)
        //// 2. Use the WFCGenerator to create a larger map (for instance, 10x10)
        //mapData = WFCGenerator2.Generate(seed, 10, 10);
        //DebugMap(mapData, "WFC Map2");
        //// 3. Apply the generated tile types to your grid tiles:
        //// (Assuming you have a function like ApplyMapToTiles(mapData) in your GridManager)
        //ApplyMapToTiles(mapData);
        yield return new WaitForSeconds(5f);
        // WFC 2
        //int[,] seed = GenerateRandomSeed(4, 4);
        DebugMap(seed, "seed");
        int[,] mapData = WFCGenerator2.Generate(seed, 10, 10);
        DebugMap(mapData, "WFC Map2");
        ApplyMapToTiles(mapData);
        Debug.Log("WFC Map 2 generated and applied to tiles.");
    }

    void CacheTiles()
    {
        gridTiles.Clear();
        Tile[] tiles = FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            gridTiles[tile.GridPos] = tile;
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        if (gridTiles.TryGetValue(gridPos, out Tile tile))
            return tile.transform.position;
        else
            return new Vector3(gridPos.x, 1, gridPos.y);
    }

    public Tile GetTileAt(Vector2Int pos) => gridTiles.TryGetValue(pos, out Tile t) ? t : null;

    public void DebugMap(int[,] map, string text)
    {
        string result = "";

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                result += map[x, y].ToString();
            }
            result += "\n";
        }
        Debug.Log($"Generated {text}:\n{result}");
    }

    public int[,] GenerateRandomSeed(int width, int height, int tileTypeCount = 3)
    {
        int[,] seed = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Grass = 0 - 5, Sand = 6 - 8, Water = 9
                int randomType = Random.Range(0, 10);

                if (randomType < 5)
                    seed[x, y] = (int)TileType.Grass;           // Grass
                else if (randomType >= 5 && randomType <= 8)
                    seed[x, y] = (int)TileType.Sand;            // Sand
                else
                    seed[x, y] = (int)TileType.Water;           // Water
                // Original
                //seed[x, y] = Random.Range(0, tileTypeCount); // Random tile type ID                
            }
        }
       
        return seed;
    }

    public void ApplyMapToTiles(int[,] mapData)
    {
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        foreach (var tile in FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Vector2Int pos = tile.GridPos;

            if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
            {
                int typeId = mapData[pos.x, pos.y];
                tile.SetType((TileType)typeId); 
            }
        }
    }

    public void RandomizeTiles()
    {
        var tiles = FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            // Grass = 0 - 5, Sand = 6 - 8, Water = 9
            int randomType = Random.Range(0, 10);

            if (randomType < 5)
                tile.SetType(TileType.Grass);           // Grass
            else if (randomType >= 5 && randomType <= 8)
                tile.SetType(TileType.Sand);            // Sand
            else
                tile.SetType(TileType.Water);           // Water
        }

        Debug.Log($"Randomized {tiles.Length} tiles.");
    }
}

public static class WFCGenerator
{
    // Represents a 2x2 or 3x3 pattern extracted from the seed
    public class Pattern
    {
        public int[,] data;
        public int weight;

        public Pattern(int[,] source, int startX, int startY, int size)
        {
            data = new int[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    data[x, y] = source[startX + x, startY + y];

            weight = 1;
        }

        // Compare two patterns for equality
        public bool Matches(Pattern other)
        {
            if (data.GetLength(0) != other.data.GetLength(0)) return false;

            for (int x = 0; x < data.GetLength(0); x++)
                for (int y = 0; y < data.GetLength(1); y++)
                    if (data[x, y] != other.data[x, y])
                        return false;

            return true;
        }
    }

    // Extract all unique NxN patterns from seed, count frequency
    private static List<Pattern> ExtractPatterns(int[,] seed, int patternSize)
    {
        List<Pattern> patterns = new List<Pattern>();

        int seedWidth = seed.GetLength(0);
        int seedHeight = seed.GetLength(1);

        for (int x = 0; x <= seedWidth - patternSize; x++)
        {
            for (int y = 0; y <= seedHeight - patternSize; y++)
            {
                Pattern newPattern = new Pattern(seed, x, y, patternSize);

                // Check if already in the list
                bool found = false;
                foreach (var p in patterns)
                {
                    if (p.Matches(newPattern))
                    {
                        p.weight++;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    patterns.Add(newPattern);
            }
        }

        return patterns;
    }

    // Generate a new map using sampled patterns
    public static int[,] Generate(int[,] seed, int outputWidth, int outputHeight, int patternSize = 2)
    {
        var patterns = ExtractPatterns(seed, patternSize);
        int[,] output = new int[outputWidth, outputHeight];

        // Fill from top-left to bottom-right using sampled patterns
        for (int x = 0; x < outputWidth; x++)
        {
            for (int y = 0; y < outputHeight; y++)
            {
                // Choose a pattern at random (weighted)
                Pattern p = ChoosePattern(patterns);

                // Pick center of pattern
                int center = patternSize / 2;
                int value = p.data[center, center];

                output[x, y] = value;
            }
        }

        return output;
    }

    // Weighted random selection
    private static Pattern ChoosePattern(List<Pattern> patterns)
    {
        int totalWeight = 0;
        foreach (var p in patterns)
            totalWeight += p.weight;

        int choice = Random.Range(0, totalWeight);
        int sum = 0;

        foreach (var p in patterns)
        {
            sum += p.weight;
            if (choice < sum)
                return p;
        }

        return patterns[0]; // fallback
    }
}

public static class WFCGenerator2
{
    // The pattern size we use for overlapping
    private const int patternSize = 2;

    // Container for a pattern extracted from the seed.
    public class Pattern
    {
        public int[,] Data;            // pattern data (patternSize x patternSize)
        public int Frequency;          // how many times this pattern appears in the seed
        public int Id;                 // unique identifier (index in the pattern list)
        public List<int> AllowedRight; // List of pattern Ids that can come to the right
        public List<int> AllowedLeft;  // ... that can come to the left
        public List<int> AllowedTop;   // ... that can come above
        public List<int> AllowedBottom;// ... that can come below
    }

    /// <summary>
    /// Generates a map based on the provided seed pattern.
    /// The seed is an int[,] (e.g. a 4x4 array), and the output map will be of dimensions outWidth x outHeight.
    /// </summary>
    public static int[,] Generate(int[,] seed, int outWidth, int outHeight)
    {
        // 1. Extract distinct patterns from the seed (using patternSize x patternSize)
        int seedWidth = seed.GetLength(0);
        int seedHeight = seed.GetLength(1);
        Dictionary<string, Pattern> patternDict = new Dictionary<string, Pattern>();

        for (int x = 0; x <= seedWidth - patternSize; x++)
        {
            for (int y = 0; y <= seedHeight - patternSize; y++)
            {
                int[,] patData = new int[patternSize, patternSize];
                for (int i = 0; i < patternSize; i++)
                {
                    for (int j = 0; j < patternSize; j++)
                    {
                        patData[i, j] = seed[x + i, y + j];
                    }
                }
                string key = PatternToString(patData);
                if (!patternDict.ContainsKey(key))
                {
                    Pattern pat = new Pattern() { Data = patData, Frequency = 1, Id = patternDict.Count };
                    patternDict[key] = pat;
                }
                else
                {
                    patternDict[key].Frequency++;
                }
            }
        }
        List<Pattern> patterns = patternDict.Values.ToList();

        // 2. For each pattern, determine which patterns are compatible as neighbors.
        foreach (var pat in patterns)
        {
            pat.AllowedRight = new List<int>();
            pat.AllowedLeft = new List<int>();
            pat.AllowedTop = new List<int>();
            pat.AllowedBottom = new List<int>();

            foreach (var other in patterns)
            {
                if (CompatibleHorizontal(pat.Data, other.Data))
                    pat.AllowedRight.Add(other.Id);
                if (CompatibleHorizontal(other.Data, pat.Data))
                    pat.AllowedLeft.Add(other.Id);
                if (CompatibleVertical(pat.Data, other.Data))
                    pat.AllowedBottom.Add(other.Id);
                if (CompatibleVertical(other.Data, pat.Data))
                    pat.AllowedTop.Add(other.Id);
            }
        }

        // 3. Create a “wave” for the output.
        // The wave dimensions correspond to positions where an entire pattern can be placed.
        int waveWidth = outWidth - patternSize + 1;
        int waveHeight = outHeight - patternSize + 1;
        List<int>[,] wave = new List<int>[waveWidth, waveHeight];
        for (int i = 0; i < waveWidth; i++)
        {
            for (int j = 0; j < waveHeight; j++)
            {
                // Initially every cell can be any of the patterns
                wave[i, j] = new List<int>(patterns.Select(pat => pat.Id));
            }
        }

        // 4. Main WFC loop: collapse cells and propagate constraints.
        bool success = true;
        while (true)
        {
            // Find the cell with the smallest non-collapsed possibility count.
            int minEntropy = int.MaxValue;
            int chosenX = -1, chosenY = -1;
            for (int i = 0; i < waveWidth; i++)
            {
                for (int j = 0; j < waveHeight; j++)
                {
                    int count = wave[i, j].Count;
                    if (count > 1 && count < minEntropy)
                    {
                        minEntropy = count;
                        chosenX = i;
                        chosenY = j;
                    }
                }
            }
            // If all cells are collapsed, we are done.
            if (chosenX == -1)
                break;

            // Collapse this cell: choose one possibility, weighted by frequency.
            List<int> options = wave[chosenX, chosenY];
            int chosenPattern = WeightedRandomChoice(options, patterns);
            wave[chosenX, chosenY] = new List<int> { chosenPattern };

            // Propagate constraints until no change.
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < waveWidth; i++)
                {
                    for (int j = 0; j < waveHeight; j++)
                    {
                        if (wave[i, j].Count == 0)
                        {
                            success = false;
                            break;
                        }
                        // Propagate to the right neighbor
                        if (i < waveWidth - 1)
                        {
                            int before = wave[i + 1, j].Count;
                            wave[i + 1, j] = wave[i + 1, j].Where(otherId =>
                            {
                                // At least one possibility in current cell must support this option.
                                return wave[i, j].Any(currentId =>
                                {
                                    Pattern current = patterns.Find(p => p.Id == currentId);
                                    return current.AllowedRight.Contains(otherId);
                                });
                            }).ToList();
                            if (wave[i + 1, j].Count != before)
                                changed = true;
                        }
                        // Left neighbor
                        if (i > 0)
                        {
                            int before = wave[i - 1, j].Count;
                            wave[i - 1, j] = wave[i - 1, j].Where(otherId =>
                            {
                                return wave[i, j].Any(currentId =>
                                {
                                    Pattern current = patterns.Find(p => p.Id == currentId);
                                    return current.AllowedLeft.Contains(otherId);
                                });
                            }).ToList();
                            if (wave[i - 1, j].Count != before)
                                changed = true;
                        }
                        // Top neighbor
                        if (j < waveHeight - 1)
                        {
                            int before = wave[i, j + 1].Count;
                            wave[i, j + 1] = wave[i, j + 1].Where(otherId =>
                            {
                                return wave[i, j].Any(currentId =>
                                {
                                    Pattern current = patterns.Find(p => p.Id == currentId);
                                    return current.AllowedTop.Contains(otherId);
                                });
                            }).ToList();
                            if (wave[i, j + 1].Count != before)
                                changed = true;
                        }
                        // Bottom neighbor
                        if (j > 0)
                        {
                            int before = wave[i, j - 1].Count;
                            wave[i, j - 1] = wave[i, j - 1].Where(otherId =>
                            {
                                return wave[i, j].Any(currentId =>
                                {
                                    Pattern current = patterns.Find(p => p.Id == currentId);
                                    return current.AllowedBottom.Contains(otherId);
                                });
                            }).ToList();
                            if (wave[i, j - 1].Count != before)
                                changed = true;
                        }
                    }
                    if (!success)
                        break;
                }
                if (!success)
                    break;
            }
            if (!success)
            {
                Debug.LogWarning("WFC failed to converge. (Try restarting the generation.)");
                break;
            }
        }

        // 5. Build the final output map.
        // For each cell in the wave, place the corresponding pattern.
        // Because patterns overlap, we simply use the top-left value of the pattern for each cell.
        int[,] output = new int[outWidth, outHeight];
        for (int i = 0; i < waveWidth; i++)
        {
            for (int j = 0; j < waveHeight; j++)
            {
                int patternId = wave[i, j][0];
                Pattern pat = patterns.Find(p => p.Id == patternId);
                for (int dx = 0; dx < patternSize; dx++)
                {
                    for (int dy = 0; dy < patternSize; dy++)
                    {
                        int outX = i + dx;
                        int outY = j + dy;
                        if (outX < outWidth && outY < outHeight)
                        {
                            output[outX, outY] = pat.Data[dx, dy];
                        }
                    }
                }
            }
        }

        return output;
    }

    #region Helper Methods

    // Converts a pattern (int[,] of size patternSize x patternSize) to a string key.
    private static string PatternToString(int[,] data)
    {
        int w = data.GetLength(0);
        int h = data.GetLength(1);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                sb.Append(data[i, j]).Append(",");
            }
        }
        return sb.ToString();
    }

    // Check whether two patterns are horizontally compatible:
    // The right column of left must equal the left column of right.
    private static bool CompatibleHorizontal(int[,] left, int[,] right)
    {
        int h = left.GetLength(1);
        int leftW = left.GetLength(0);
        for (int j = 0; j < h; j++)
        {
            if (left[leftW - 1, j] != right[0, j])
                return false;
        }
        return true;
    }

    // Check whether two patterns are vertically compatible:
    // The bottom row of top must equal the top row of bottom.
    private static bool CompatibleVertical(int[,] top, int[,] bottom)
    {
        int w = top.GetLength(0);
        int topH = top.GetLength(1);
        for (int i = 0; i < w; i++)
        {
            if (top[i, topH - 1] != bottom[i, 0])
                return false;
        }
        return true;
    }

    // Choose one option from a list of pattern ids, weighted by frequency.
    private static int WeightedRandomChoice(List<int> options, List<Pattern> patterns)
    {
        float total = 0;
        foreach (int id in options)
        {
            Pattern pat = patterns.Find(p => p.Id == id);
            total += pat.Frequency;
        }
        float r = Random.value * total;
        foreach (int id in options)
        {
            Pattern pat = patterns.Find(p => p.Id == id);
            r -= pat.Frequency;
            if (r <= 0)
                return id;
        }
        return options[0];
    }

    #endregion
}
