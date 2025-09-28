using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SectionType { Outside, City, Plains, Road, Building, Shack }
public enum CityStyle { Auto, Grid, Organic }

[System.Serializable]
public struct CityData
{
    public Vector2Int center;
    public int size;

    public CityData(Vector2Int c, int s)
    {
        center = c;
        size = s;
    }
}

[System.Serializable]
public class SectionSettings
{
    public SectionType type;
    public int minSections;
    public int maxSections;
    public int minSize;
    public int maxSize;
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
    public float gridCityBias = 0.4f; // 70% grid, 30% organic

    // Internal data
    private Dictionary<Vector2Int, SectionType> sectionGrid = new();
    private Dictionary<Vector2Int, Cube> cubeInstances = new();
    private Dictionary<SectionType, List<Cube>> cubeDict = new();
    private List<CityData> cityCenters = new();

    public void GenerateTerrain()
    {
        sectionGrid = new();

        BuildCubeDictionary();
        GenerateOutsideRing(); //generates outside ring, and places it 
        GenerateAllSections(); //sets sectionGrid values;
        //GenerateAllCubes(); //creates cubes based on sectionGrid

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
            //Debug.Log($"[CubeMatch] No strict match at {gridPos}, fallback used.");
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
                    //Debug.Log("spawning outside cube at " + x + "," + y);

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

        // Sort by load order
        sectionSettings.Sort((a, b) => a.loadOrder.CompareTo(b.loadOrder));

        foreach (var section in sectionSettings)
        {
            int sectionCount = Random.Range(section.minSections, section.maxSections + 1);
            for (int i = 0; i < sectionCount; i++)
            {
                int size = Random.Range(section.minSize, section.maxSize + 1);

                int spawnMargin = sectionSpawnMargin + (size / 2);
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
                        if (IsCityFarEnough(center, size))
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

                        // If the section explicitly defines a style, use it
                        if (section.cityStyle == CityStyle.Auto)
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
                            GenerateGridCity(center, size, section.cityToRoadChance, section.minRoadSpacing, section.maxRoadSpacing);
                        else
                            GenerateOrganicCity(center, size);

                        cityCenters.Add(new CityData(center, size));

                        break;

                    case SectionType.Plains:
                        //GeneratePlain(center, size);
                        break;
                }
            }
        }
    }

    private bool IsCityFarEnough(Vector2Int newCenter, int newSize)
    {
        int newHalf = newSize / 2;

        foreach (var city in cityCenters)
        {
            int existingHalf = city.size / 2;

            float dist = Vector2Int.Distance(newCenter, city.center);
            if (dist < cityMinDistance + newHalf + existingHalf)
            {
                return false;
            }
        }
        return true;
    }

    /*
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

    */

    private void GenerateAllCubes()
    {

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

    private void GenerateGridCity(Vector2Int center, int size, float cityChance, int minRoadSpacing, int maxRoadSpacing)
    {
        int half = size / 2;

        int roadSpacing = Mathf.Clamp(Random.Range(minRoadSpacing, maxRoadSpacing + 1), 0, half);

        Debug.Log("generating grid city at " + center + " with size of " + size + ", and a spacing of " + roadSpacing);

        // --- PASS 1: Roads (on road lines only) ---
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);

                bool isRoadLine = (x % roadSpacing == 0 || y % roadSpacing == 0);

                if (isRoadLine)
                {
                    if(Random.value > cityChance)
                    {
                        sectionGrid[pos] = SectionType.Road;
                        SpawnCubeAt(pos, SectionType.Road);
                    }        
                }
            }
        }

        // --- PASS 2: Cities (on road lines only) ---
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);

                bool isRoadLine = (x % roadSpacing == 0 || y % roadSpacing == 0);

                if (isRoadLine)
                {
                    if (sectionGrid.ContainsKey(pos))
                        continue;

                    sectionGrid[pos] = SectionType.City;
                    SpawnCubeAt(pos, SectionType.City);
                }
            }
        }

        // --- PASS 3: Fill Remaining (non-road-line cells) ---
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);

                // Skip if already assigned in pass 1
                if (sectionGrid.ContainsKey(pos))
                    continue;

                sectionGrid[pos] = SectionType.Plains;
                SpawnCubeAt(pos, SectionType.Plains);
            }
        }

    }
    /*
    // 2. Organic city (cluster-based with winding roads)
    private void GenerateOrganicCity(Vector2Int center, int size)
    {
        Debug.Log("generating organic city at " + center + " with size of " + size);

        int half = size / 2;

        List<Vector2Int> buildings = new();

        int buildingLimit = (half * half) / 2;

        int buildingCount = Random.Range(2, buildingLimit);

        // Step 1: Place buildings randomly
        int placed = 0;
        int attempts = 0;
        while (placed < buildingCount && attempts < buildingCount * 10)
        {
            attempts++;
            Vector2Int randomCell = WorldToGrid(
                (Vector2)GridToWorld(center) + Random.insideUnitCircle * (size * cubeSize)
            );

            if (!buildings.Contains(randomCell))
            {
                buildings.Add(randomCell);
                sectionGrid[randomCell] = SectionType.City;
                SpawnCubeAt(randomCell, SectionType.City);
                placed++;
            }
        }

        // Step 2: Connect buildings with winding roads
        for (int i = 0; i < buildings.Count - 1; i++)
        {
            Vector2Int start = buildings[i];
            Vector2Int end = buildings[i + 1];
            Vector2Int current = start;

            int safety = 0; // prevents infinite loops
            while (current != end && safety++ < size * size)
            {
                // Move toward end, but with some randomness
                if (Random.value > 0.5f)
                    current.x += current.x < end.x ? 1 : (current.x > end.x ? -1 : 0);
                else
                    current.y += current.y < end.y ? 1 : (current.y > end.y ? -1 : 0);

                if (!sectionGrid.ContainsKey(current))
                {
                    sectionGrid[current] = SectionType.Road;
                    SpawnCubeAt(current, SectionType.Road);
                }
            }
        }
    }*/

    private void GenerateOrganicCity(Vector2Int center, int size)
    {
        Debug.Log("Generating organic city at " + center + " with size " + size);

        int half = size / 2;
        List<Vector2Int> buildings = new();

        int buildingLimit = Mathf.Max(2, (half * half) / 2);
        int buildingCount = Random.Range(2, buildingLimit);

        // --- Step 1: Place buildings randomly ---
        int placed = 0;
        int attempts = 0;
        while (placed < buildingCount && attempts < buildingCount * 10)
        {
            attempts++;
            Vector2Int randomCell = WorldToGrid(
                (Vector2)GridToWorld(center) + Random.insideUnitCircle * (half * cubeSize)
            );

            // Clamp inside world bounds
            randomCell.x = Mathf.Clamp(randomCell.x, -worldSize / 2, worldSize / 2);
            randomCell.y = Mathf.Clamp(randomCell.y, -worldSize / 2, worldSize / 2);

            if (!buildings.Contains(randomCell))
            {
                buildings.Add(randomCell);
                sectionGrid[randomCell] = SectionType.City;
                SpawnCubeAt(randomCell, SectionType.City);
                placed++;
            }
        }

        // Shuffle buildings so road connections aren’t predictable
        buildings = buildings.OrderBy(b => Random.value).ToList();

        // --- Step 2: Connect buildings to nearest neighbor ---
        HashSet<Vector2Int> connected = new();
        connected.Add(buildings[0]);

        List<Vector2Int> remaining = buildings.Skip(1).ToList();

        while (remaining.Count > 0)
        {
            Vector2Int currentBuilding = connected.OrderBy(c => Random.value).First();

            // Find closest remaining building
            Vector2Int closest = remaining.OrderBy(b => Vector2Int.Distance(currentBuilding, b)).First();

            // Connect the two
            CreateRoad(currentBuilding, closest, size);

            connected.Add(closest);
            remaining.Remove(closest);
        }

        // --- Step 3: Optional random side branches ---
        float branchChance = 0.3f; // 30% chance
        foreach (var b1 in buildings)
        {
            foreach (var b2 in buildings)
            {
                if (b1 == b2) continue;
                if (Random.value < branchChance)
                {
                    CreateRoad(b1, b2, size / 2); // shorter safety limit for branches
                }
            }
        }

        // Register city in cityCenters
        cityCenters.Add(new CityData(center, size));
    }

    // --- Helper method to create winding road between two points ---
    private void CreateRoad(Vector2Int start, Vector2Int end, int safetyLimit)
    {
        Vector2Int current = start;
        int safety = 0;

        while (current != end && safety++ < safetyLimit * safetyLimit)
        {
            // Randomly move along X or Y
            if (Random.value > 0.5f)
                current.x += current.x < end.x ? 1 : (current.x > end.x ? -1 : 0);
            else
                current.y += current.y < end.y ? 1 : (current.y > end.y ? -1 : 0);

            // Occasionally take a diagonal step
            if (Random.value < 0.1f)
            {
                if (current.x != end.x && current.y != end.y)
                {
                    current.x += current.x < end.x ? 1 : -1;
                    current.y += current.y < end.y ? 1 : -1;
                }
            }

            // Place road if cell is empty or plains/shack
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
    private void GeneratePlain(Vector2Int center, int size)
    {
        int half = size / 2;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
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
        //Debug.Log(prefab.name);
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
