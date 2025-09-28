using System.Collections.Generic;
using UnityEngine;

public enum SectionType { Outside, City, Plains, Road, Building, Shack }

[System.Serializable]
public class SectionSettings
{
    public SectionType type;
    public int minSections;
    public int maxSections;
    public int minSize;
    public int maxSize;
    public int loadOrder;
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
        GeneratePlains();
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
                        GenerateCity(center, size);
                        cityCenters.Add(center);
                        break;
                    case SectionType.Plains:
                        GeneratePlain(center, size);
                        break;
                }
            }
        }
    }

    private void ConnectCitiesWithPlainsRoads()
    {
        for (int i = 0; i < cityCenters.Count - 1; i++)
        {
            Vector2Int start = cityCenters[i];
            Vector2Int end = cityCenters[i + 1];

            Vector2Int current = start;
            while (current != end)
            {
                Vector2Int step = new Vector2Int(
                    current.x < end.x ? 1 : current.x > end.x ? -1 : 0,
                    current.y < end.y ? 1 : current.y > end.y ? -1 : 0
                );

                current += step;

                if (!sectionGrid.ContainsKey(current) || sectionGrid[current] == SectionType.Plains)
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

    #region Section Generators
    private void GenerateCity(Vector2Int center, int size)
    {
        int half = size / 2;
        int roadSpacing = Random.Range(minRoadSpacing, maxRoadSpacing + 1);

        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (pos.x < 0 || pos.y < 0 || pos.x >= worldSize || pos.y >= worldSize) continue;

                bool isRoad = (x % roadSpacing == 0 || y % roadSpacing == 0);

                // Small plazas inside city
                if (!isRoad && Random.value < 0.05f)
                    sectionGrid[pos] = SectionType.Plains;
                else
                    sectionGrid[pos] = isRoad ? SectionType.Road : SectionType.Building;

                SpawnCubeAt(pos, sectionGrid[pos]);
            }
        }
    }

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
