using System.Collections.Generic;
using UnityEngine;

public enum SectionType { Outside, City, Plains, Road, Building, Shack }
public enum CityStyle { Auto, Grid, Organic }


[System.Serializable]
public class SectionSettings
{
    public SectionType type;
    public int minSections;
    public int maxSections;
    public int minSize;
    public int maxSize;
    public int loadOrder;

    public CityStyle cityStyle = CityStyle.Auto;
}

public class TerrainBuilder : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSize = 50;
    public int outsideRingThickness = 1;
    public int sectionSpawnMargin = 10;
    [SerializeField] private float cubeSize = 25f;

    [Header("Road Settings")]
    public int minRoadSpacing = 3;
    public int maxRoadSpacing = 6;

    [Header("Sections")]
    public List<SectionSettings> sectionSettings;

    [Header("Prefabs")]
    public List<Cube> allCubes;

    [Header("City Settings")]
    public int cityMinDistance = 10;
    [Range(0f, 1f)]
    public float gridCityBias = 0.4f; // 70% grid, 30% organic

    // Internal data
    private Dictionary<Vector2Int, SectionType> sectionGrid = new();
    private Dictionary<Vector2Int, Cube> cubeInstances = new();
    private Dictionary<SectionType, List<Cube>> cubeDict = new();
    private List<Vector2Int> cityCenters = new();

    public void GenerateTerrain()
    {
        sectionGrid = new();

        BuildCubeDictionary();
        GenerateOutsideRing();
        GenerateAllSections();
        ConnectCitiesWithPlainsRoads();
        //GeneratePlains();
        FillEmptyWithPlains();
        PopulatePlainsWithShacks();
    }

    #region Cube Dictionary
    private void BuildCubeDictionary()
    {
        foreach (Cube cube in allCubes)
        {
            if (!cubeDict.ContainsKey(cube.cubeType))
                cubeDict[cube.cubeType] = new List<Cube>();
            cubeDict[cube.cubeType].Add(cube);
        }
    }

    public void ClearTerrain()
    {
        // Destroy all spawned cube GameObjects
        foreach (var cube in cubeInstances.Values)
        {
            if (cube != null)
                Destroy(cube.gameObject);
        }

        // Clear all internal tracking structures
        cubeInstances.Clear();
        sectionGrid.Clear();
        cityCenters.Clear();

        Debug.Log("[TerrainBuilder] Terrain cleared.");
    }

    private Cube GetMatchingCube(Vector2Int gridPos, SectionType fallbackType)
    {
        Dictionary<SideDirection, SideType> requiredSides = new();

        foreach (SideDirection dir in System.Enum.GetValues(typeof(SideDirection)))
        {
            Vector2Int neighborPos = gridPos + DirFromSide(dir);
            if (cubeInstances.TryGetValue(neighborPos, out Cube neighbor))
            {
                SideDirection opposite = OppositeDirection(dir);
                cubeSide neighborSide = neighbor.sides.Find(s => s.sideDirection == opposite);
                if (neighborSide != null)
                    requiredSides[dir] = neighborSide.sideType;
            }
        }

        List<Cube> candidates = new();
        if (cubeDict.TryGetValue(fallbackType, out List<Cube> cubeList))
        {
            foreach (Cube prefab in cubeList)
            {
                bool matches = true;
                foreach (var req in requiredSides)
                {
                    cubeSide side = prefab.sides.Find(s => s.sideDirection == req.Key);
                    if (side == null || side.sideType != req.Value)
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[CubeMatch] No strict match at {gridPos}, fallback used.");
            return GetRandomCube(fallbackType); 
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Cube GetRandomCube(SectionType type)
    {
        if (!cubeDict.ContainsKey(type) || cubeDict[type].Count == 0)
            return null;
        return cubeDict[type][Random.Range(0, cubeDict[type].Count)];
    }
    #endregion

    #region World Generation
    private void GenerateOutsideRing()
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                bool isOutside =
                    (x < outsideRingThickness || y < outsideRingThickness ||
                     x >= worldSize - outsideRingThickness ||
                     y >= worldSize - outsideRingThickness);

                if (isOutside)
                {
                    Vector2Int pos = new(x, y);
                    sectionGrid[pos] = SectionType.Outside;
                    SpawnCubeAt(pos, SectionType.Outside);
                }
            }
        }
    }

    private void GenerateAllSections()
    {
        // Sort by load order
        sectionSettings.Sort((a, b) => a.loadOrder.CompareTo(b.loadOrder));

        foreach (var section in sectionSettings)
        {
            int sectionCount = Random.Range(section.minSections, section.maxSections + 1);
            for (int i = 0; i < sectionCount; i++)
            {
                int size = Random.Range(section.minSize, section.maxSize + 1);
                Vector2Int center = new Vector2Int(
                    Random.Range(sectionSpawnMargin, worldSize - sectionSpawnMargin),
                    Random.Range(sectionSpawnMargin, worldSize - sectionSpawnMargin)
                );

                switch (section.type)
                {
                    case SectionType.City:
                        CityStyle chosenStyle;

                        // If the section explicitly defines a style, use it
                        if (section.cityStyle != CityStyle.Grid && section.cityStyle != CityStyle.Organic)
                        {
                            // Otherwise pick based on global bias
                            chosenStyle = (Random.value < gridCityBias)
                                ? CityStyle.Grid
                                : CityStyle.Organic;
                        }
                        else
                        {
                            chosenStyle = section.cityStyle;
                        }

                        if (chosenStyle == CityStyle.Grid)
                            GenerateGridCity(center, size);
                        else
                            GenerateOrganicCity(center, size);

                        cityCenters.Add(center);
                        break;

                    case SectionType.Plains:
                        GeneratePlain(center, size);
                        break;
                }
            }
        }
    }

    private bool IsCityFarEnough(Vector2Int newCenter, int newSize)
    {
        int halfSize = newSize / 2;

        foreach (var existing in cityCenters)
        {
            float dist = Vector2Int.Distance(newCenter, existing);
            if (dist < cityMinDistance + halfSize)
            {
                return false;
            }
        }
        return true;
    }

    private void ConnectCitiesWithPlainsRoads()
    {
        for (int i = 0; i < cityCenters.Count - 1; i++)
        {
            Vector2Int start = cityCenters[i];
            Vector2Int end = cityCenters[i + 1];
            Vector2Int current = start;

            // Decide randomly whether to prioritize x or y first (adds variation)
            bool prioritizeX = Random.value > 0.5f;

            while (current != end)
            {
                if (prioritizeX)
                {
                    // Move in X direction if not aligned
                    if (current.x != end.x)
                    {
                        current.x += current.x < end.x ? 1 : -1;
                    }
                    else if (current.y != end.y)
                    {
                        current.y += current.y < end.y ? 1 : -1;
                    }
                }
                else
                {
                    // Move in Y direction if not aligned
                    if (current.y != end.y)
                    {
                        current.y += current.y < end.y ? 1 : -1;
                    }
                    else if (current.x != end.x)
                    {
                        current.x += current.x < end.x ? 1 : -1;
                    }
                }

                // Occasionally insert a random jog to make roads less straight
                if (Random.value < 0.1f) // 10% chance
                {
                    if (Random.value > 0.5f && current.x != end.x)
                    {
                        current.x += current.x < end.x ? 1 : -1;
                    }
                    else if (current.y != end.y)
                    {
                        current.y += current.y < end.y ? 1 : -1;
                    }
                }

                // Mark road if not already city/road
                if (!sectionGrid.ContainsKey(current) ||
                    sectionGrid[current] == SectionType.Plains ||
                    sectionGrid[current] == SectionType.Shack)
                {
                    sectionGrid[current] = SectionType.Road;
                    SpawnCubeAt(current, SectionType.Road);
                }
            }
        }
    }

    private void GeneratePlains()
    {
        foreach (var kvp in sectionGrid)
        {
            if (kvp.Value == 0)
            {
                Vector2Int pos = kvp.Key;
                sectionGrid[pos] = SectionType.Plains;
                SpawnCubeAt(pos, SectionType.Plains);
            }
        }
    }

    private void FillEmptyWithPlains()
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                Vector2Int pos = new(x, y);
                if (!sectionGrid.ContainsKey(pos))
                {
                    sectionGrid[pos] = SectionType.Plains;
                    SpawnCubeAt(pos, SectionType.Plains);
                }
            }
        }
    }
    #endregion

    private void GenerateGridCity(Vector2Int center, int size)
    {
        int half = size / 2;
        int roadSpacing = Random.Range(minRoadSpacing, maxRoadSpacing + 1);

        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (pos.x < 0 || pos.y < 0 || pos.x >= worldSize || pos.y >= worldSize) continue;

                bool isRoadLine = (x % roadSpacing == 0 || y % roadSpacing == 0);

                if (isRoadLine)
                {
                    sectionGrid[pos] = (Random.value < 0.7f)
                        ? SectionType.Road
                        : SectionType.Building;
                }
                else
                {
                    float roll = Random.value;
                    if (roll < 0.05f) sectionGrid[pos] = SectionType.Plains;
                    else if (roll < 0.10f) sectionGrid[pos] = SectionType.Shack;
                    else sectionGrid[pos] = SectionType.Building;
                }

                SpawnCubeAt(pos, sectionGrid[pos]);
            }
        }
    }

    // 2. Organic city (cluster-based with winding roads)
    private void GenerateOrganicCity(Vector2Int center, int size, int clusterCount = 4)
    {
        int half = size / 2;
        List<Vector2Int> clusters = new();

        // Step 1: Place clusters of buildings
        for (int i = 0; i < clusterCount; i++)
        {
            Vector2Int clusterCenter = center + new Vector2Int(
                Random.Range(-half, half),
                Random.Range(-half, half)
            );

            clusters.Add(clusterCenter);

            int clusterSize = Random.Range(2, 5);
            for (int x = -clusterSize; x <= clusterSize; x++)
            {
                for (int y = -clusterSize; y <= clusterSize; y++)
                {
                    Vector2Int pos = clusterCenter + new Vector2Int(x, y);
                    if (pos.x < 0 || pos.y < 0 || pos.x >= worldSize || pos.y >= worldSize) continue;

                    sectionGrid[pos] = SectionType.Building;
                    SpawnCubeAt(pos, SectionType.Building);
                }
            }
        }

        // Step 2: Connect clusters with winding roads
        for (int i = 0; i < clusters.Count - 1; i++)
        {
            Vector2Int start = clusters[i];
            Vector2Int end = clusters[i + 1];
            Vector2Int current = start;

            while (current != end)
            {
                if (Random.value > 0.5f)
                    current.x += current.x < end.x ? 1 : -1;
                else
                    current.y += current.y < end.y ? 1 : -1;

                if (!sectionGrid.ContainsKey(current))
                {
                    sectionGrid[current] = SectionType.Road;
                    SpawnCubeAt(current, SectionType.Road);
                }
            }
        }
    }

    #region Section Generators
    private void GeneratePlain(Vector2Int center, int size)
    {
        int half = size / 2;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (pos.x < 0 || pos.y < 0 || pos.x >= worldSize || pos.y >= worldSize) continue;

                if (!sectionGrid.ContainsKey(pos))
                {
                    sectionGrid[pos] = SectionType.Plains;
                    SpawnCubeAt(pos, SectionType.Plains);
                }
            }
        }
    }

    private void PopulatePlainsWithShacks(float spawnChance = 0.25f)
    {
        foreach (var kvp in sectionGrid)
        {
            if (kvp.Value == SectionType.Plains)
            {
                Vector2Int pos = kvp.Key;
                if (Random.value < spawnChance)
                {
                    sectionGrid[pos] = SectionType.Shack;
                    SpawnCubeAt(pos, SectionType.Shack);
                }
            }
        }
    }
    #endregion

    #region Helpers
    private void SpawnCubeAt(Vector2Int gridPos, SectionType type)
    {
        Cube prefab = GetMatchingCube(gridPos, type);
        if (prefab != null)
        {
            GameObject obj = Instantiate(
                prefab.cubePrefab,
                GridToWorld(gridPos),
                Quaternion.identity,
                transform
            );
            cubeInstances[gridPos] = obj.GetComponent<Cube>();
        }
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cubeSize, gridPos.y * cubeSize, 0f);
    }

    private Vector2Int DirFromSide(SideDirection dir) => dir switch
    {
        SideDirection.Up => new Vector2Int(0, 1),
        SideDirection.Down => new Vector2Int(0, -1),
        SideDirection.Left => new Vector2Int(-1, 0),
        SideDirection.Right => new Vector2Int(1, 0),
        _ => Vector2Int.zero,
    };

    private SideDirection OppositeDirection(SideDirection dir) => dir switch
    {
        SideDirection.Up => SideDirection.Down,
        SideDirection.Down => SideDirection.Up,
        SideDirection.Left => SideDirection.Right,
        SideDirection.Right => SideDirection.Left,
        _ => dir,
    };
    #endregion
}
