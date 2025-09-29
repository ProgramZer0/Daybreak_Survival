using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SectionType { Outside, City, Plains, Road, Shack }
public enum CityStyle { Auto, Grid, Organic }

[System.Serializable]
public struct CityData
{
    public Vector2Int center;
    public int radius; // now radius instead of size

    public CityData(Vector2Int c, int r)
    {
        center = c;
        radius = r;
    }
}

[System.Serializable]
public class SectionSettings
{
    public SectionType type;
    public int minSections;
    public int maxSections;
    public int minSize; // interpreted as radius
    public int maxSize; // interpreted as radius
    public int loadOrder;

    public float cityToRoadChance = 0.7f;
    public CityStyle cityStyle = CityStyle.Auto;
    public int minRoadSpacing = 1;
    public int maxRoadSpacing = 8;
}

public class TerrainBuilder : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSize = 50;
    public int outsideRingThickness = 1;
    public int sectionSpawnMargin = 10;
    [SerializeField] private float cubeSize = 25f;

    [Header("Sections")]
    public List<SectionSettings> sectionSettings;

    [Header("Prefabs")]
    public List<Cube> allCubes;

    [Header("City Settings")]
    public int cityMinDistance = 10;
    [SerializeField] int maxCityPlacementTries = 10;
    [Range(0f, 1f)]
    public float gridCityBias = 0.4f;

    // Internal data
    private Dictionary<Vector2Int, SectionType> sectionGrid = new();
    private Dictionary<Vector2Int, Cube> cubeInstances = new();
    private Dictionary<SectionType, List<Cube>> cubeDict = new();
    private List<CityData> cityCenters = new();

    public void GenerateTerrain()
    {
        sectionGrid = new();

        BuildCubeDictionary();
        GenerateOutsideRing();
        GenerateAllSections();

        //GenerateAllCubes();
        //ConnectCitiesWithPlainsRoads();
        //GeneratePlains();
        //FillEmptyWithPlains();
        //PopulatePlainsWithShacks();
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
        foreach (var cube in cubeInstances.Values)
        {
            if (cube != null)
                Destroy(cube.gameObject);
        }

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
            return GetRandomCube(fallbackType);

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
        int halfWorld = 0 - (worldSize / 2);

        for (int x = halfWorld; x <= (worldSize / 2); x++)
        {
            for (int y = halfWorld; y <= (worldSize / 2); y++)
            {
                bool isOutside = false;

                if (x < halfWorld + outsideRingThickness || x > (worldSize / 2) - outsideRingThickness)
                    isOutside = true;
                if (y < halfWorld + outsideRingThickness || y > (worldSize / 2) - outsideRingThickness)
                    isOutside = true;

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
        int halfWorldNeg = 0 - (worldSize / 2);
        int halfWorldPos = (worldSize / 2);

        sectionSettings.Sort((a, b) => a.loadOrder.CompareTo(b.loadOrder));

        foreach (var section in sectionSettings)
        {
            int sectionCount = Random.Range(section.minSections, section.maxSections + 1);
            for (int i = 0; i < sectionCount; i++)
            {
                int radius = Random.Range(section.minSize, section.maxSize + 1);

                int spawnMargin = sectionSpawnMargin + radius;
                bool validCenter = false;
                int count = 0;
                Vector2Int center = new Vector2Int();
                while (!validCenter && count < maxCityPlacementTries)
                {
                    center = new Vector2Int(
                        Random.Range(halfWorldNeg + spawnMargin, (halfWorldPos - spawnMargin) + 1),
                        Random.Range(halfWorldNeg + spawnMargin, (halfWorldPos - spawnMargin) + 1)
                    );

                    if (section.type == SectionType.City)
                    {
                        if (IsCityFarEnough(center, radius))
                            validCenter = true;
                        else
                            center = new Vector2Int();
                    }
                    else
                        validCenter = true;
                    count++;
                }

                if (center == Vector2Int.zero) continue;

                switch (section.type)
                {
                    case SectionType.City:
                        CityStyle chosenStyle;

                        if (section.cityStyle == CityStyle.Auto)
                        {
                            chosenStyle = (Random.value < gridCityBias)
                                ? CityStyle.Grid
                                : CityStyle.Organic;
                        }
                        else
                        {
                            chosenStyle = section.cityStyle;
                        }

                        if (chosenStyle == CityStyle.Grid)
                            GenerateGridCity(center, radius, section.cityToRoadChance, section.minRoadSpacing, section.maxRoadSpacing);
                        else
                            GenerateOrganicCity(center, radius);

                        cityCenters.Add(new CityData(center, radius));
                        break;

                    case SectionType.Plains:
                        //GeneratePlain(center, radius);
                        break;
                }
            }
        }
    }

    private bool IsCityFarEnough(Vector2Int newCenter, int newRadius)
    {
        foreach (var city in cityCenters)
        {
            float dist = Vector2Int.Distance(newCenter, city.center);
            if (dist < cityMinDistance + newRadius + city.radius)
                return false;
        }
        return true;
    }
    #endregion

    private void GenerateGridCity(Vector2Int center, int radius, float cityChance, int minRoadSpacing, int maxRoadSpacing)
    {
        int adjustedRadius = (radius % 2 == 0) ? radius : radius + 1;

        // Collect valid spacings (divisors of adjustedSize)
        List<int> validSpacings = new(); 

        for (int i = minRoadSpacing; i <= maxRoadSpacing; i++) 
        { 
            if (i > 0 && adjustedRadius % i == 0) 
                validSpacings.Add(i); 
        } 
            
        // Safety: fallback if no valid spacing found
        if (validSpacings.Count == 0)
            validSpacings.Add(minRoadSpacing); 

        int roadSpacing = validSpacings[Random.Range(0, validSpacings.Count)];

        Debug.Log($"[GridCity] center {center}, radius {radius}, adjusting Radius {adjustedRadius}, spacing {roadSpacing}");

        // Roads
        for (int x = -adjustedRadius; x <= adjustedRadius; x++)
        {
            for (int y = -adjustedRadius; y <= adjustedRadius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                bool isRoadLine = (x % roadSpacing == 0 || y % roadSpacing == 0);

                if (isRoadLine && Random.value > cityChance)
                {
                    sectionGrid[pos] = SectionType.Road;
                    SpawnCubeAt(pos, SectionType.Road);
                }
            }
        }

        // Cities
        for (int x = -adjustedRadius; x <= adjustedRadius; x++)
        {
            for (int y = -adjustedRadius; y <= adjustedRadius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                bool isRoadLine = (x % roadSpacing == 0 || y % roadSpacing == 0);

                if (isRoadLine && !sectionGrid.ContainsKey(pos))
                {
                    sectionGrid[pos] = SectionType.City;
                    SpawnCubeAt(pos, SectionType.City);
                }
            }
        }

        // Fill Plains
        for (int x = -adjustedRadius; x <= adjustedRadius; x++)
        {
            for (int y = -adjustedRadius; y <= adjustedRadius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (!sectionGrid.ContainsKey(pos))
                {
                    sectionGrid[pos] = SectionType.Plains;
                    SpawnCubeAt(pos, SectionType.Plains);
                }
            }
        }
    }

    private void GenerateOrganicCity(Vector2Int center, int radius)
    {
        Debug.Log($"[OrganicCity] center {center}, radius {radius}");

        List<Vector2Int> buildings = new();
        int buildingLimit = Mathf.Max(2, (radius * radius) / 2);
        int buildingCount = Random.Range(2, buildingLimit);

        int placed = 0;
        int attempts = 0;
        while (placed < buildingCount && attempts < buildingCount * 10)
        {
            attempts++;
            Vector2Int randomCell = WorldToGrid(
                (Vector2)GridToWorld(center) + Random.insideUnitCircle * (radius * cubeSize)
            );

            if (!buildings.Contains(randomCell))
            {
                buildings.Add(randomCell);
                sectionGrid[randomCell] = SectionType.City;
                SpawnCubeAt(randomCell, SectionType.City);
                placed++;
            }
        }

        buildings = buildings.OrderBy(b => Random.value).ToList();

        // Track building connections
        Dictionary<Vector2Int, int> connectionCount = new();
        foreach (var b in buildings) connectionCount[b] = 0;

        HashSet<Vector2Int> connected = new();
        connected.Add(buildings[0]);
        List<Vector2Int> remaining = buildings.Skip(1).ToList();

        // Connect buildings (main MST)
        while (remaining.Count > 0)
        {
            Vector2Int currentBuilding = connected.OrderBy(c => Random.value).First();
            Vector2Int closest = remaining
                .OrderBy(b => Vector2Int.Distance(currentBuilding, b))
                .First();

            CreateRoadIfAllowed(currentBuilding, closest, radius, connectionCount);

            connected.Add(closest);
            remaining.Remove(closest);
        }

        // Optional side branches
        float branchChance = 0.3f;
        foreach (var b1 in buildings)
        {
            foreach (var b2 in buildings)
            {
                if (b1 == b2) continue;
                if (connectionCount[b1] >= 1 || connectionCount[b2] >= 1) continue;
                if (Random.value < branchChance)
                {
                    CreateRoadIfAllowed(b1, b2, radius / 2, connectionCount);
                }
            }
        }
    }

    // Only create a road if both buildings have < 2 connections
    private void CreateRoadIfAllowed(Vector2Int start, Vector2Int end, int safetyLimit, Dictionary<Vector2Int, int> connectionCount)
    {
        if (connectionCount[start] >= 1 || connectionCount[end] >= 1)
        {
            Debug.Log("location " + start + " or " + end + " is full");
            return;
        }

        CreateRoad(start, end, safetyLimit);

        connectionCount[start]++;
        connectionCount[end]++;
    }

    private void CreateRoad(Vector2Int start, Vector2Int end, int safetyLimit)
    {
        Vector2Int current = start;
        int safety = 0;

        while (current != end && safety++ < safetyLimit * safetyLimit)
        {
            if (Random.value < 0.9f)
            {
                if (Mathf.Abs(end.x - current.x) > Mathf.Abs(end.y - current.y))
                    current.x += (end.x > current.x) ? 1 : -1;
                else if (end.y != current.y)
                    current.y += (end.y > current.y) ? 1 : -1;
            }
            else
            {
                if (Random.value > 0.5f && current.x != end.x)
                    current.x += (end.x > current.x) ? 1 : -1;
                else if (current.y != end.y)
                    current.y += (end.y > current.y) ? 1 : -1;
            }

            if (!sectionGrid.ContainsKey(current) ||
                sectionGrid[current] == SectionType.Plains ||
                sectionGrid[current] == SectionType.Shack)
            {
                sectionGrid[current] = SectionType.Road;
                SpawnCubeAt(current, SectionType.Road);
            }
        }
    }

    #region Section Generators
    private void GeneratePlain(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);

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

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cubeSize),
            Mathf.RoundToInt(worldPos.y / cubeSize)
        );
    }
    #endregion
}