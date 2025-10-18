using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SectionType { Empty, Outside, City, Plains, Road, Shack, Highway, rualBuildings, rualRoads, parks, roadConnections, anything}
public enum CityStyle { Auto, Grid, Organic }

[System.Serializable]
public struct RoadData
{
    public List<Vector2Int> roads;

    public RoadData(List<Vector2Int> initRoads)
    {
        roads = initRoads;
    }
}

[System.Serializable]
public struct CityData
{
    public Vector2Int center;
    public int radius; // now radius instead of size
    public CityStyle type;
    public int highwayConnections;

    public CityData(Vector2Int c, int r, CityStyle t)
    {
        center = c;
        radius = r;
        type = t;
        highwayConnections = 0;
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
    public List<Cube> testCubes;

    [Header("City Settings")]
    public int cityMinDistance = 10;
    [SerializeField] int maxCityPlacementTries = 10;
    [Range(0f, 1f)]
    public float gridCityBias = 0.4f;
    public float shackChance = 0.25f;

    // Internal data
    private SectionType[,] sectionGrid;
    private Cube[,] cubeInstances;

    private int gridOffset;

    private Dictionary<SectionType, List<Cube>> cubeDict = new();
    private Dictionary<SectionType, List<Cube>> cubeTestDict = new();
    private List<CityData> cityCenters = new();
    private List<RoadData> roadListObjs = new();
    private List<Vector2Int> cityTilesToCheck = new();
    private List<Vector2Int> highwayRoads = new();
    private List<CityData> connectedCities;
    
    public List<GameObject> enemySpawnersObj = new();
    public List<GameObject> weaponSpawnerobj = new();
    public List<GameObject> lifeStyleSpawnerobj = new();
    public List<GameObject> collectorsSpawnerobj = new();
    public List<GameObject> ammoSpawnerobj = new(); 


    public bool isRunning = false;
    public bool isDeleting = false;
    public bool useTest = false;

    public void GenerateTerrain()
    {   
        isRunning = true;
        Debug.Log("[TerrainBuilder] Terrain started.");
        gridOffset = worldSize / 2;

        sectionGrid = new SectionType[worldSize, worldSize];
        cubeInstances = new Cube[worldSize, worldSize];
        connectedCities = new List<CityData>();

        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                sectionGrid[x, y] = SectionType.Empty;
                cubeInstances[x, y] = null;
            }
        }

        BuildCubeDictionary();
        GenerateOutsideRing();
        GenerateAllSections();
        StartCoroutine(SpawnAllCubes());
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

        foreach (Cube cube in testCubes)
        {
            if (!cubeTestDict.ContainsKey(cube.cubeType))
                cubeTestDict[cube.cubeType] = new List<Cube>();
            cubeTestDict[cube.cubeType].Add(cube);
        }
    }

    public void ClearTerrain()
    {
        // Kick off the clear process
        StartCoroutine(ClearTerrainRoutine());
    }

    private IEnumerator ClearTerrainRoutine()
    {
        // If a delete is already running, wait until it's done
        while (isDeleting)
            yield return null;

        isDeleting = true;

        // Do deletion
        yield return StartCoroutine(DeleteAllCubes());

        cityCenters.Clear();
        roadListObjs.Clear();
        enemySpawnersObj.Clear();
        weaponSpawnerobj.Clear();

        Debug.Log("[TerrainBuilder] Terrain cleared.");
    }

    private IEnumerator DeleteAllCubes()
    {
        if (cubeInstances != null)
        {
            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    if (cubeInstances[x, y] != null)
                        Destroy(cubeInstances[x, y].gameObject);

                    cubeInstances[x, y] = null;
                }
                // Yield after each row to avoid frame hitch
                yield return null;
            }
        }

        isDeleting = false;
    }

    private Cube GetMatchingCube(Vector2Int gridPos, SectionType fallbackType, List<Vector2Int> cityPlaced = null)
    {
        // Step 1: Gather neighbor SectionTypes directly
        Dictionary<SideDirection, SectionType> neighborTypes = new();

        foreach (SideDirection dir in System.Enum.GetValues(typeof(SideDirection)))
        {
            Vector2Int neighborPos = gridPos + DirFromSide(dir);

            if (!InBounds(neighborPos))
            {
                neighborTypes[dir] = SectionType.Outside;
                continue;
            }

            neighborTypes[dir] = SectionAt(neighborPos);
        }

        // Step 2: Get prefabs of target section type
        if (!cubeDict.TryGetValue(fallbackType, out var cubeList) || cubeList.Count == 0)
        {
            Debug.LogError($"no cubes found for type {fallbackType}");
            return null;
        }

        // Step 3: Check and score each prefab
        List<(Cube cube, int exactMatches, int anythingUsed)> scored = new();

        foreach (Cube prefab in cubeList)
        {
            if (prefab.cubeType != fallbackType)
                continue;

            bool matches = true;
            int exactMatches = 0;
            int anythingUsed = 0;

            foreach (var side in prefab.sides)
            {
                if (!neighborTypes.TryGetValue(side.sideDirection, out var neighborType))
                    continue;

                if (side.sideType.Contains(neighborType))
                {
                    exactMatches++; // Perfect match
                }
                else if (side.sideType.Contains(SectionType.anything))
                {
                    anythingUsed++; // Used "anything" to cover mismatch
                }
                else
                {
                    matches = false;
                    break; // Total mismatch
                }
            }

            if (matches)
                scored.Add((prefab, exactMatches, anythingUsed));
        }

        // Step 4: Choose the cube with the highest exact match count, lowest "anything" usage
        Cube result = null;

        if (scored.Count > 0)
        {
            int maxExact = scored.Max(s => s.exactMatches);
            var bestByExact = scored.Where(s => s.exactMatches == maxExact).ToList();

            int minAnything = bestByExact.Min(s => s.anythingUsed);
            var bestFinal = bestByExact.Where(s => s.anythingUsed == minAnything).ToList();

            result = bestFinal[Random.Range(0, bestFinal.Count)].cube;

            //Debug.Log($"Selected cube at {gridPos} with {maxExact} exact matches and {minAnything} 'anything' sides.");
        }
        else
        {
            //Debug.LogWarning($"No matching cubes found at {gridPos}, using random fallback.");
            result = GetRandomCube(fallbackType);
        }

        // Step 5: Track city placements
        if (result != null && cityPlaced != null)
        {
            if (fallbackType == SectionType.City || fallbackType == SectionType.Road || fallbackType == SectionType.parks)
                cityPlaced.Add(gridPos);
        }

        return result;
    }

    private Cube GetRandomCube(SectionType type)
    {
        if (useTest)
        {
            if (!cubeTestDict.ContainsKey(type) || cubeTestDict[type].Count == 0)
                return null;
            return cubeTestDict[type][Random.Range(0, cubeTestDict[type].Count)];
        }
        else
        {
            if (!cubeDict.ContainsKey(type) || cubeDict[type].Count == 0)
                return null;
            return cubeDict[type][Random.Range(0, cubeDict[type].Count)];
        }
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
                    //Debug.Log("205");
                    if (InBounds(pos)) SectionAt(pos) = SectionType.Outside;
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
                        {
                            radius = GenerateGridCity(center, radius, section.cityToRoadChance, section.minRoadSpacing, section.maxRoadSpacing);
                            cityCenters.Add(new CityData(center, radius, CityStyle.Grid));
                        }
                        else
                        {
                            GenerateOrganicCity(center, radius);
                            cityCenters.Add(new CityData(center, radius, CityStyle.Organic));
                        }

                        break;

                    case SectionType.Plains:
                        //GeneratePlain(center, radius);
                        break;
                }
            }
        }

        ConnectAllCities(new Vector2Int(0 - gridOffset, 0 - gridOffset));
        if (CheckAllCityConnections())
            ConnectAllCities(new Vector2Int(0 + gridOffset, 0 + gridOffset));
        if (CheckAllCityConnections())
            ConnectAllCities(new Vector2Int(0 - gridOffset, 0 + gridOffset));
        if (CheckAllCityConnections())
            ConnectAllCities(new Vector2Int(0 + gridOffset, 0 - gridOffset));
        if (CheckAllCityConnections())
            ConnectAllCities(Vector2Int.zero);
        if (CheckAllCityConnections())
            ConnectHighwayToCities();

        FillEmptyWithPlains();
        PopulatePlainsWithShacks(shackChance);

        Debug.Log("[TerrainBuilder] Sections Generated.");
    }

    IEnumerator SpawnAllCubes()
    {
        int half = worldSize / 2;
        cityTilesToCheck.Clear();

        for (int gx = -half; gx <= half; gx++)
        {
            for (int gy = -half; gy <= half; gy++)
            {
                Vector2Int gridPos = new Vector2Int(gx, gy);
                if (!InBounds(gridPos)) continue;

                SectionType type = SectionAt(gridPos);
                if (type == SectionType.Empty) continue;

                // Skip city tiles but record them
                if (type == SectionType.City)
                {
                    cityTilesToCheck.Add(gridPos);
                    continue;
                }
                Cube prefab;

                if (useTest)
                    prefab = GetRandomCube(type);
                else
                    prefab = GetMatchingCube(gridPos, type);

                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab.cubePrefab, GridToWorld(gridPos), Quaternion.identity, transform);
                    SetCubeAt(gridPos, obj.GetComponent<Cube>());

                    foreach (GameObject o in obj.GetComponent<Cube>().enemySpawners)
                        enemySpawnersObj.Add(o);

                    foreach (GameObject o in obj.GetComponent<Cube>().weaponSpawners)
                        weaponSpawnerobj.Add(o);

                    foreach (GameObject o in obj.GetComponent<Cube>().lifeStyleSpawners)
                        lifeStyleSpawnerobj.Add(o);

                    foreach (GameObject o in obj.GetComponent<Cube>().ammoSpawners)
                        ammoSpawnerobj.Add(o);
                }
            }

            yield return null;
        }

        // After normal cubes, process cities
        ProcessCityTiles();

        isRunning = false;
    }
    private void ProcessCityTiles()
    {
        foreach (var cityPos in cityTilesToCheck)
        {
            GameObject prefabToSpawn = null;

            foreach (SideDirection dir in System.Enum.GetValues(typeof(SideDirection)))
            {
                Vector2Int neighborPos = cityPos + DirFromSide(dir);
                if (!InBounds(neighborPos))
                {
                    Debug.LogWarning($"not in bounds at {neighborPos}");
                    continue;
                }
                Cube neighbor = CubeAt(neighborPos);
                if (neighbor == null) continue;

                // Look at the side of neighbor facing the city
                SideDirection opposite = OppositeDirection(dir);
                cubeSide side = neighbor.sides.Find(s => s.sideDirection == opposite);

                if (side != null && side.isDefinedConnected && side.PrefabNeeded != null)
                {
                    prefabToSpawn = side.PrefabNeeded;
                    break; // we found a valid connection
                }
            }

            if (prefabToSpawn == null)
            {
                if(useTest)
                    prefabToSpawn = GetRandomCube(SectionType.parks).cubePrefab;
                else
                    prefabToSpawn = GetRandomParkPrefab();
            }
            else
                if (useTest)
                    prefabToSpawn = GetRandomCube(SectionType.City).cubePrefab;


            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, GridToWorld(cityPos), Quaternion.identity, transform);
                SetCubeAt(cityPos, obj.GetComponent<Cube>());
            }
        }

        cityTilesToCheck.Clear();
    }

    private GameObject GetRandomParkPrefab()
    {
        if (!cubeDict.TryGetValue(SectionType.parks, out var parks) || parks.Count == 0)
            return null;

        Cube parkCube = parks[Random.Range(0, parks.Count)];
        return parkCube.cubePrefab;
    }

    private bool CheckAllCityConnections()
    {
        bool hasACityThatHasNoRoads = false;

        foreach (CityData city in cityCenters)
        {
            if (!isNearType(city, SectionType.Highway))
            {
                Debug.Log($" city {city.center} is not near road ");
                hasACityThatHasNoRoads = true;
            }
                
        }

        return hasACityThatHasNoRoads;
    }

    
    private void ConnectAllCities(Vector2Int closeTo)
    {
        if (cityCenters.Count < 2)
        {
            Debug.Log("has less than 2 cities skipping roads");
            return;
        }

        var cities = cityCenters.OrderBy(_ => Random.value).ToList();
        CityData from = cities[0];

        float distance = float.MaxValue;

        foreach (CityData c in cities)
        {
            if (c.type != CityStyle.Grid) continue;
            
            if(Vector2Int.Distance(closeTo, c.center) < distance)
            {
                distance = Vector2Int.Distance(closeTo, c.center);
                from = c;
            }
        }
        if(!connectedCities.Contains(from))
            connectedCities.Add(from);

        Debug.Log($"city count {cities.Count}, from city {from.center}");

        
        foreach (CityData city in cities)
        {
            if (connectedCities.Contains(city)) continue;
            var to = city;
            int radius = Mathf.Max(from.radius, to.radius);
            CreateCityRoad(from, to);

            Debug.Log($"adding city {from.center} to {to.center}");
            
            if(isNearType(city, SectionType.Highway))
            {
                connectedCities.Add(city);
                Debug.Log($"city has highway {city.center}");

            }
        }
    }

    private bool isNearType(CityData city, SectionType type)
    {
        return city.type == CityStyle.Grid
            ? isNearTypeGrid(city, type)
            : isNearTypeOrganic(city, type);
    }

    private bool isNearTypeOrganic(CityData city, SectionType type)
    {
        // 1. Start with all rural buildings and roads in range
        HashSet<Vector2Int> ruralTiles = new();

        for (int x = city.center.x - city.radius; x <= city.center.x + city.radius; x++)
        {
            for (int y = city.center.y - city.radius; y <= city.center.y + city.radius; y++)
            {
                Vector2Int tile = new(x, y);
                if (!InBounds(tile)) continue;

                if (IsType(tile, SectionType.rualBuildings) || IsType(tile, SectionType.rualRoads))
                    ruralTiles.Add(tile);
            }
        }

        // 2. Expand to include any connected rural roads/buildings
        bool expanded = true;
        while (expanded)
        {
            expanded = false;
            // Take a snapshot so we don’t modify while iterating
            List<Vector2Int> currentTiles = new(ruralTiles);

            foreach (var tile in currentTiles)
            {
                foreach (var neighbor in GetNeighbors(tile))
                {
                    if (!InBounds(neighbor)) continue;

                    if ((IsType(neighbor, SectionType.rualBuildings) || IsType(neighbor, SectionType.rualRoads))
                        && !ruralTiles.Contains(neighbor))
                    {
                        ruralTiles.Add(neighbor);
                        expanded = true;
                    }
                }
            }
        }

        // 3. Check if any rural tile borders the requested type (e.g., highway)
        foreach (var tile in ruralTiles)
        {
            Debug.Log($"[OrganicCheck] {city.center} has tile {tile}");
            if (isAdjacentToType(tile, type))
            {
                Debug.Log($"[OrganicCheck] {city.center} is near {type}");
                return true;
            }
        }

        return false;
    }

    private bool isNearTypeGrid(CityData city, SectionType type)
    {
        int radius;
        if (city.type == CityStyle.Grid)
            radius = city.radius + 1;
        else
            radius = 3;

        //if any section grid near the city in the city.radius is the section type then we return true otherwise return false.
        for (int i = city.center.x - radius; i < city.center.x + radius; i++)
        {
            for (int j = city.center.y - radius; j < city.center.y + radius; j++)
            {
                if (IsType(new Vector2Int(i, j), type))
                {
                    Debug.Log($"city has highway {city.center} at {i},{j}");
                    return true;
                }
            }
        }
        
        return false;
    }

    private void ConnectHighwayToCities()
    {
        List<CityData> disconnectedCities = new();
        foreach (CityData city in cityCenters)
        {
            if (!isNearType(city, SectionType.Highway))
            {
                disconnectedCities.Add(city);
            }

        }

        foreach (CityData c in disconnectedCities)
        {
            Vector2Int? highwayPos = FindNearestHighway(c.center, 30);
            if (highwayPos == null)
            {
                Debug.Log($"No highway found near city at {c.center}");
                continue;
            }

            // halfway point between city center and highway
            Vector2 halfway = Vector2.Lerp(c.center, highwayPos.Value, 0.5f);
            Vector2Int end = new Vector2Int(Mathf.RoundToInt(halfway.x), Mathf.RoundToInt(halfway.y));

            // Now find the nearest road *starting* from that halfway point
            Vector2Int? start = FindNearestRoad(end, (int)Vector2Int.Distance(end, c.center));
            if (start == null)
            {
                Debug.Log($"No road found near halfway point {end} for city {c.center}");
                continue;
            }

            CreateHighwaysFromTo(start.Value, end);
        }
    }
    private Vector2Int? FindNearestRoad(Vector2Int start, int searchRadius)
    {
        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int pos = new Vector2Int(start.x + dx, start.y + dy);
                    if (!InBounds(pos)) continue;

                    SectionType type = SectionAt(pos);
                    if (type == SectionType.rualRoads)
                        return pos;
                }
            }
        }

        return null; // none found
    }
    private Vector2Int? FindNearestHighway(Vector2Int start, int searchRadius)
    {
        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int pos = new Vector2Int(start.x + dx, start.y + dy);
                    if (!InBounds(pos)) continue;

                    SectionType type = SectionAt(pos);
                    if (type == SectionType.Highway)
                        return pos;
                }
            }
        }

        return null; // none found
    }
    private void CreateHighwaysFromTo(Vector2Int start, Vector2Int end)
    {

        Vector2 endir = (end - start);

        Vector2Int IntDir = new Vector2Int(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y));

        if (Random.value > 0.5f)
        {
            if (IntDir.x != 0)
                IntDir.y = 0;
        }
        else
        {
            if (IntDir.y != 0)
                IntDir.x = 0;
        }

        Vector2Int current = start + IntDir;

        Debug.Log($"current is {current} start city is {start} goal is {end}");

        int safety = 0, safetyLimit = 5000;
        bool isFirst = true;

        while (current != end && safety++ < safetyLimit)
        {
            Vector2Int delta = end - current;

            if (isFirst)
            {
                SetSection(current, SectionType.Highway);
                highwayRoads.Add(current);
                isFirst = false;
                continue;
            }

            Vector2Int step;

            // Weighted random movement for natural flow
            bool moveX = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? Random.value > 0.5f
                : Random.value > 0.7f;

            step = moveX
                ? new Vector2Int(Mathf.Clamp(delta.x, -1, 1), 0)
                : new Vector2Int(0, Mathf.Clamp(delta.y, -1, 1));

            if (isAdjacentToType(current, SectionType.Highway, highwayRoads))
            {
                Debug.Log($"hit valid highway at {current} for city {start} to {end}");
                return;
            }

            Vector2Int next = current + step;

            if (!InBounds(next))
            {
                Debug.Log($"Out of bounds at {next} for city {start} to {end}");
                break;
            }

            // --- Avoid passing too close to other cities ---
            bool nearOtherCity = cityCenters.Any(c => Vector2Int.Distance(next, c.center) < c.radius + 2);

            if (nearOtherCity)
            {
                CityData closest = cityCenters.OrderBy(c => Vector2Int.Distance(next, c.center)).First();
                Vector2 away = ((Vector2)current - (Vector2)closest.center).normalized;
                next += new Vector2Int(Mathf.RoundToInt(away.x), Mathf.RoundToInt(away.y));
                Debug.Log($"Steering around city at {closest.center}");
            }

            SectionType section = SectionAt(next);

            current = next;
        }
    }

    private void CreateCityRoad(CityData startCityData, CityData endCityData)
    {
        Vector2Int startCity = startCityData.center;
        Vector2Int endCity = endCityData.center;
        int startRadius = startCityData.radius;
        int endRadius = endCityData.radius;

        highwayRoads.Clear();

        Vector2 endCityDir = (endCity - startCity);
        endCityDir.Normalize();

        Vector2Int IntDir = new Vector2Int(Mathf.RoundToInt(endCityDir.x), Mathf.RoundToInt(endCityDir.y));

        if(Random.value > 0.5f)
        {
            if (IntDir.x != 0)
                IntDir.y = 0;
        }
        else
        {
            if (IntDir.y != 0)
                IntDir.x = 0;
        }

        IntDir = Checkside(startCityData, IntDir);

        //Debug.LogError($"int dir is {IntDir}");
        Vector2Int current = startCityData.center + (IntDir * (startCityData.radius + 1));

        Debug.Log($"current is {current} start city is {startCity} goal is {endCity} and int dir is {IntDir}");

        int safety = 0, safetyLimit = 5000;
        bool isFirst = true;
        bool isSecond = true;
        
        if(endCityData.type == CityStyle.Organic)
        {
            endCity = FindNearestOfType(endCity, SectionType.rualRoads);

            while (current != endCity && safety++ < safetyLimit)
            {
                Vector2Int delta = endCity - current;

                if (isFirst)
                {
                    SetSection(current, SectionType.Highway);
                    highwayRoads.Add(current);
                    isFirst = false;
                    continue;
                }

                Vector2Int step;

                // Weighted random movement for natural flow
                if (isSecond)
                {
                    step = IntDir;
                    isSecond = false;
                }
                else
                {
                    bool moveX = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? Random.value > 0.5f
                    : Random.value > 0.7f;

                    step = moveX
                        ? new Vector2Int(Mathf.Clamp(delta.x, -1, 1), 0)
                        : new Vector2Int(0, Mathf.Clamp(delta.y, -1, 1));
                }

                if (isAdjacentToType(current, SectionType.Highway, highwayRoads))
                {
                    Debug.Log($"hit valid highway at {current} for city {startCity} to {endCity}");
                    return;
                }

                Vector2Int next = current + step;

                if (!InBounds(next))
                {
                    Debug.Log($"Out of bounds at {next} for city {startCity} to {endCity}");
                    break;
                }

                // --- Avoid passing too close to other cities ---
                var potentialCities = cityCenters
    .Where(c => c.center != endCityData.center)
    .OrderBy(c => Vector2Int.Distance(next, c.center))
    .ToList();

                if (potentialCities.Count > 0)
                {
                    CityData closest = potentialCities[0];
                    bool nearOtherCity = false;

                    if (closest.type == CityStyle.Organic)
                    {
                        // Circular check for organic cities
                        nearOtherCity = Vector2Int.Distance(next, closest.center) < closest.radius + 2;
                    }
                    else
                    {
                        // Square check for grid cities
                        int halfSize = closest.radius;
                        Vector2Int min = closest.center - new Vector2Int(halfSize, halfSize);
                        Vector2Int max = closest.center + new Vector2Int(halfSize, halfSize);

                        // Slight buffer (like +1) to start steering *before* collision
                        if (next.x >= min.x - 1 && next.x <= max.x + 1 &&
                            next.y >= min.y - 1 && next.y <= max.y + 1)
                        {
                            nearOtherCity = true;
                        }
                    }

                    if (nearOtherCity)
                    {
                        // Compute direction away from the closest city’s bounds/center
                        Vector2 away;
                        if (closest.type == CityStyle.Organic)
                        {
                            away = ((Vector2)current - (Vector2)closest.center).normalized;
                        }
                        else
                        {
                            // For grid cities, steer away based on which side we’re nearest
                            int dx = 0, dy = 0;
                            int halfSize = closest.radius;

                            if (next.x < closest.center.x - halfSize)
                                dx = -1;
                            else if (next.x > closest.center.x + halfSize)
                                dx = 1;

                            if (next.y < closest.center.y - halfSize)
                                dy = -1;
                            else if (next.y > closest.center.y + halfSize)
                                dy = 1;

                            away = new Vector2(dx, dy).normalized;
                        }

                        next += new Vector2Int(Mathf.RoundToInt(away.x), Mathf.RoundToInt(away.y));
                        Debug.Log($"Steering around {(closest.type == CityStyle.Organic ? "organic" : "grid")} city at {closest.center}");
                    }
                }

                SectionType section = SectionAt(next);

                // Stop early if hitting a road outside start radius
                if ((section == SectionType.Road || section == SectionType.rualRoads)
                    && Vector2Int.Distance(next, startCity) > startRadius + 2)
                {
                    Debug.Log($"Stopping early near {current}, hit road outside city radius for {startCity}");
                    return;
                }

                // --- Merge highways into rural buildings/roads ---
                if ((section == SectionType.rualRoads || section == SectionType.rualBuildings)
                    && Vector2Int.Distance(next, endCity) < endRadius + 4)
                {
                    SetSection(current, SectionType.rualRoads);
                    Debug.Log($"Merged highway into rural road at {current}");
                    return;
                }

                // --- Normal highway painting --- 
                if (section == SectionType.Empty || section == SectionType.Plains || section == SectionType.rualBuildings)
                {
                    SetSection(next, SectionType.Highway);
                    highwayRoads.Add(next);
                }

                if ((isAdjacentToType(next, SectionType.rualBuildings) || isAdjacentToType(next, SectionType.rualRoads)) && Vector2Int.Distance(next, startCity) > startRadius + 2)
                {
                    SetSection(next, SectionType.rualRoads);
                    highwayRoads.Remove(next);
                }

                // --- Stop if too close to city edge ---
                if (isAdjacentToType(next, SectionType.rualBuildings) && Vector2Int.Distance(next, startCity) > startRadius)
                {

                    Vector2Int? nearestRoad = FindNearestRoad(next, 15);
                    if (nearestRoad.HasValue)
                        ConnectToRoad(next, nearestRoad.Value);

                    Debug.Log($"Stopped near city edge at {next}, connecting to nearest road");
                    break;
                }

                current = next;
            }
        }
        else
        {
            while (current != endCity && safety++ < safetyLimit)
            {
                Vector2Int delta = endCity - current;

                if (isFirst)
                {
                    SetSection(current, SectionType.Highway);
                    highwayRoads.Add(current);
                    isFirst = false;
                    continue;
                }

                Vector2Int step;

                // Weighted random movement for natural flow
                if (isSecond)
                {
                    step = IntDir;
                    isSecond = false;
                }
                else
                {
                    bool moveX = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? Random.value > 0.5f
                    : Random.value > 0.7f;

                    step = moveX
                        ? new Vector2Int(Mathf.Clamp(delta.x, -1, 1), 0)
                        : new Vector2Int(0, Mathf.Clamp(delta.y, -1, 1));
                }

                if (isAdjacentToType(current, SectionType.Highway, highwayRoads) && Vector2Int.Distance(current, startCity) > startRadius + 3)
                {
                    Debug.Log($"hit valid highway at {current} for city {startCity} to {endCity}");
                    return;
                }

                Vector2Int next = current + step;

                if (!InBounds(next))
                {
                    Debug.Log($"Out of bounds at {next} for city {startCity} to {endCity}");
                    break;
                }

                // --- Avoid passing too close to other cities ---
                // --- Avoid passing too close to other cities ---
                var potentialCities = cityCenters
    .Where(c => c.center != endCityData.center)
    .OrderBy(c => Vector2Int.Distance(next, c.center))
    .ToList();

                if (potentialCities.Count > 0)
                {
                    CityData closest = potentialCities[0];
                    bool nearOtherCity = false;

                    if (closest.type == CityStyle.Organic)
                    {
                        // Circular check for organic cities
                        nearOtherCity = Vector2Int.Distance(next, closest.center) < closest.radius + 2;
                    }
                    else
                    {
                        // Square check for grid cities
                        int halfSize = closest.radius;
                        Vector2Int min = closest.center - new Vector2Int(halfSize, halfSize);
                        Vector2Int max = closest.center + new Vector2Int(halfSize, halfSize);

                        // Slight buffer (like +1) to start steering *before* collision
                        if (next.x >= min.x - 1 && next.x <= max.x + 1 &&
                            next.y >= min.y - 1 && next.y <= max.y + 1)
                        {
                            nearOtherCity = true;
                        }
                    }

                    if (nearOtherCity)
                    {
                        // Compute direction away from the closest city’s bounds/center
                        Vector2 away;
                        if (closest.type == CityStyle.Organic)
                        {
                            away = ((Vector2)current - (Vector2)closest.center).normalized;
                        }
                        else
                        {
                            // For grid cities, steer away based on which side we’re nearest
                            int dx = 0, dy = 0;
                            int halfSize = closest.radius;

                            if (next.x < closest.center.x - halfSize)
                                dx = -1;
                            else if (next.x > closest.center.x + halfSize)
                                dx = 1;

                            if (next.y < closest.center.y - halfSize)
                                dy = -1;
                            else if (next.y > closest.center.y + halfSize)
                                dy = 1;

                            away = new Vector2(dx, dy).normalized;
                        }

                        next += new Vector2Int(Mathf.RoundToInt(away.x), Mathf.RoundToInt(away.y));
                        Debug.Log($"Steering around {(closest.type == CityStyle.Organic ? "organic" : "grid")} city at {closest.center}");
                    }
                }

                SectionType section = SectionAt(next);

                // Stop early if hitting a road outside start radius
                if ((section == SectionType.Road || section == SectionType.rualRoads)
                    && Vector2Int.Distance(next, startCity) > startRadius + 2)
                {
                    Debug.Log($"Stopping early near {current}, hit road outside city radius for {startCity}");
                    return;
                }

                // --- Merge highways into rural buildings/roads ---
                if ((section == SectionType.rualRoads || section == SectionType.rualBuildings)
                    && Vector2Int.Distance(next, endCity) < endRadius + 4)
                {
                    SetSection(current, SectionType.rualRoads);
                    SetSection(next, SectionType.rualRoads);
                    Debug.Log($"Merged highway into rural road at {current}");
                    return;
                }

                // --- Normal highway painting ---
                if (section == SectionType.Empty || section == SectionType.Plains || section == SectionType.rualBuildings)
                {
                    SetSection(next, SectionType.Highway);
                    highwayRoads.Add(next);
                }

                // --- Stop if too close to city edge ---
                if (isAdjacentToType(next, SectionType.rualBuildings) && Vector2Int.Distance(next, startCity) > startRadius)
                {
                    Vector2Int? nearestRoad = FindNearestRoad(next, 15);
                    if (nearestRoad.HasValue)
                        ConnectToRoad(next, nearestRoad.Value);

                    Debug.Log($"Stopped near city edge at {next}, connecting to nearest road");
                    break;
                }

                if (next == endCity)
                    SetSection(next, SectionType.rualRoads);

                current = next;
            }
        }
    }

    private Vector2Int Checkside(CityData startCityData, Vector2Int dir)
    {

        Vector2Int intReturn = startCityData.center + (dir * (startCityData.radius + 1));
        if (SectionAt(intReturn) == SectionType.Highway)
        {
            if (dir.x == 0 && dir.y == 1)
            {
                dir.x = 1;
                dir.y = 0;
            }
            else if (dir.x == 1 && dir.y == 0)
            {
                dir.x = 0;
                dir.y = -1;
            }
            else if (dir.x == 0 && dir.y == -1)
            {
                dir.x = -1;
                dir.y = 0;
            }
            else if (dir.x == -1 && dir.y == 0)
            {
                dir.x = 0;
                dir.y = 1;
            }

            intReturn = startCityData.center + ( dir * (startCityData.radius + 1));

            if (SectionAt(intReturn) == SectionType.Highway)
            {
                if (dir.x == 0 && dir.y == 1)
                {
                    dir.x = 1;
                    dir.y = 0;
                }
                else if (dir.x == 1 && dir.y == 0)
                {
                    dir.x = 0;
                    dir.y = -1;
                }
                else if (dir.x == 0 && dir.y == -1)
                {
                    dir.x = -1;
                    dir.y = 0;
                }
                else if (dir.x == -1 && dir.y == 0)
                {
                    dir.x = 0;
                    dir.y = 1;
                }

                intReturn = startCityData.center + (dir * (startCityData.radius + 1));
                if (SectionAt(intReturn) == SectionType.Highway)
                {
                    if (dir.x == 0 && dir.y == 1)
                    {
                        dir.x = 1;
                        dir.y = 0;
                    }
                    else if (dir.x == 1 && dir.y == 0)
                    {
                        dir.x = 0;
                        dir.y = -1;
                    }
                    else if (dir.x == 0 && dir.y == -1)
                    {
                        dir.x = -1;
                        dir.y = 0;
                    }
                    else if (dir.x == -1 && dir.y == 0)
                    {
                        dir.x = 0;
                        dir.y = 1;
                    }

                    intReturn = startCityData.center + (dir * (startCityData.radius + 1));
                    if (SectionAt(intReturn) == SectionType.Highway)
                    {
                        if (dir.x == 0 && dir.y == 1)
                        {
                            dir.x = 1;
                            dir.y = 0;
                        }
                        else if (dir.x == 1 && dir.y == 0)
                        {
                            dir.x = 0;
                            dir.y = -1;
                        }
                        else if (dir.x == 0 && dir.y == -1)
                        {
                            dir.x = -1;
                            dir.y = 0;
                        }
                        else if (dir.x == -1 && dir.y == 0)
                        {
                            dir.x = 0;
                            dir.y = 1;
                        }
                    }
                }
            }
        }

        return dir;
    }

    
    private void ConnectToRoad(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int safety = 0, safetyLimit = 1000;

        while (current != to && safety++ < safetyLimit)
        {
            Vector2Int step = new Vector2Int(
                Mathf.Clamp(to.x - current.x, -1, 1),
                Mathf.Clamp(to.y - current.y, -1, 1)
            );

            current += step;

            if (!InBounds(current)) break;

            SectionType section = SectionAt(current);
            if (section == SectionType.Empty || section == SectionType.Plains)
                SetSection(current, SectionType.Highway);

            if (section == SectionType.Road || section == SectionType.rualRoads)
                break;
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
    private void FillEmptyWithPlains()
    {
        int halfWorld = worldSize / 2;

        for (int x = -halfWorld; x <= halfWorld; x++)
        {
            for (int y = -halfWorld; y <= halfWorld; y++)
            {
                Vector2Int pos = new(x, y);
                if (IsEmpty(pos))
                {
                    //Debug.Log("387");
                    SectionAt(pos) = SectionType.Plains;
                }
            }
        }
    }

    private void PopulatePlainsWithShacks(float spawnChance = 0.25f)
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                Vector2Int pos = new Vector2Int(x - gridOffset, y - gridOffset);

                if (InBounds(pos) && SectionAt(pos) == SectionType.Plains && Random.value < spawnChance)
                {
                    if (IsSurroundedByPlains(pos, 4))
                    {
                        SetSection(pos, SectionType.Shack);
                    }
                }
            }
        }
    }

    private bool IsSurroundedByPlains(Vector2Int center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue; // skip center itself

                Vector2Int checkPos = new Vector2Int(center.x + dx, center.y + dy);

                if (!InBounds(checkPos)) return false; // out of bounds not allowed
                if (SectionAt(checkPos) != SectionType.Plains) return false;
            }
        }
        return true;
    }

    private int GenerateGridCity(Vector2Int center, int radius, float cityChance, int minRoadSpacing, int maxRoadSpacing, float parkPercent = 0.3f)
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
                
                if (InBounds(pos) && isRoadLine) SectionAt(pos) = SectionType.Road;
            }
        }

        // Cities
        for (int x = -adjustedRadius; x <= adjustedRadius; x++)
        {
            for (int y = -adjustedRadius; y <= adjustedRadius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);


                if (IsEmpty(pos))
                {
                    //Debug.Log("457");
                    if(Random.value < parkPercent)
                        SectionAt(pos) = SectionType.parks;
                    else 
                        SectionAt(pos) = SectionType.City;
                }
            }
        }

        return adjustedRadius;
    }

    private void GenerateOrganicCity(Vector2Int center, int radius)
    {
        Debug.Log($"[OrganicCity] center {center}, radius {radius}");

        roadListObjs.Clear();           
        List<Vector2Int> buildings = new();
        int buildingLimit = Mathf.Max(2, (radius * radius) / 2);
        int buildingCount = Mathf.Max(2, Random.Range(2, buildingLimit));

        int placed = 0;
        int attempts = 0;
        while (placed < buildingCount && attempts < buildingCount * 20)
        {
            Vector2Int randomCell = WorldToGrid(
            (Vector2)GridToWorld(center) + Random.insideUnitCircle * (radius * cubeSize));

            if (!InBounds(randomCell))
                continue;

            // Skip if already placed or adjacent to any building
            if (buildings.Contains(randomCell))
                continue;

            if (isAdjacentToType(randomCell, SectionType.rualBuildings))
                continue;

            // Skip if not empty
            if (!IsEmpty(randomCell))
                continue;

            // Place the building
            buildings.Add(randomCell);
            SetSection(randomCell, SectionType.rualBuildings);
            placed++;
        }

        Dictionary<Vector2Int, bool> isConnected = new();

        buildings = buildings.OrderBy(b => Random.value).ToList();

        foreach (var building in buildings)
        {
            //Debug.Log($"looking at building {building} for city {center}");

            //step 1  if building is next to building or a road stop
            Vector2Int adjacentTile = isAdjacentToCellVector(building);
            if (adjacentTile != Vector2Int.zero)
            {
                //Debug.Log("building " + building + " is adjecent to something ");
                roadListObjs.Add(new RoadData(new List<Vector2Int> { building, adjacentTile }));
                continue;
            }   
            
            //step 3 road goes straight towards another building (if there is a building that has a connection we go towards that if not pick random one)
            Vector2Int target = PickTargetBuilding(buildings, building, center, isConnected);

            //step 2 road starts from direction towards center
            Vector2Int startingDir;
            if (building.x > target.x)
            {
                //to the right of target

                if (building.y > target.y)
                {
                    //building is above
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.left;
                    else
                        startingDir = Vector2Int.down;
                }
                else
                {
                    //building is below
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.left;
                    else
                        startingDir = Vector2Int.up;
                }
            }
            else
            {
                //to the left of target

                if (building.y > target.y)
                {
                    //building is above
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.right;
                    else
                        startingDir = Vector2Int.down;
                }
                else
                {
                    //building is below
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.right;
                    else
                        startingDir = Vector2Int.up;
                }
            }

            //Debug.Log("direction towards center is  " + startingDir.ToString());

            //Debug.Log("target is " + target);

            Vector2Int endPoint = CreateRoad((building + startingDir), target, radius, building);

            if (buildings.Contains(endPoint))
            {
                if (!isConnected.ContainsKey(building))
                    isConnected[building] = true;
                if (!isConnected.ContainsKey(endPoint))
                    isConnected[endPoint] = true;
            }
        }

        if (roadListObjs.Count <= 1) return;

        Debug.Log($"listing all road obj for city {center}");
        int roadCount = 1;
        foreach(RoadData rd in roadListObjs)
        {
            foreach(Vector2Int vec in rd.roads)
            {
                Debug.Log($"vector {vec} is in road obj {roadCount} in city {center}");
            }
            roadCount++;
        }

        //Step 5 connect any local roads to center
        ConnectAllRoadSegmentsOptimized();
    }
    private void ConnectAllRoadSegmentsOptimized()
    {
        if (roadListObjs.Count <= 1) return;

        // Separate connected / disconnected roads
        List<RoadData> connected = new();
        List<RoadData> disconnected = new(roadListObjs);

        // Start with the first road segment
        connected.Add(disconnected[0]);
        disconnected.RemoveAt(0);

        // Build a global hash set for fast adjacency checks
        HashSet<Vector2Int> connectedTiles = new HashSet<Vector2Int>();
        foreach (var vec in connected[0].roads)
            connectedTiles.Add(vec);

        int maxIterations = 1000;
        int iteration = 0;
        bool added;

        do
        {

            added = false;
            iteration++;

            if (iteration > maxIterations)
            {
                Debug.LogWarning("[TerrainBuilder] ConnectAllRoadSegments reached max iterations. Forcing remaining connections.");
                break;
            }

            for (int i = disconnected.Count - 1; i >= 0; i--)
            {
                RoadData road = disconnected[i];

                if (IsAdjacentToConnectedTiles(road, connectedTiles))
                {
                    // Add to connected
                    connected.Add(road);
                    disconnected.RemoveAt(i);

                    // Add all tiles to the global set
                    foreach (var vec in road.roads)
                        connectedTiles.Add(vec);

                    added = true;
                }
            }

        } while (added);

        // Connect any remaining disconnected segments
        foreach (var road in disconnected)
        {
            Vector2Int from = road.roads[Random.Range(0, road.roads.Count)];
            Vector2Int to = FindNearestRoadVector(from, connectedTiles);

            // Create a direct road between them
            CreateRoad(from, to, 50, from, false);

            // Add tiles to connected set
            foreach (var vec in road.roads)
                connectedTiles.Add(vec);

            connected.Add(road);
            Debug.Log($"adding road from {from} to {to}");
        }
    }
    private Vector2Int PickTargetBuilding(List<Vector2Int> buildings, Vector2Int current, Vector2Int center, Dictionary<Vector2Int, bool> connected)
    {
        // Prefer already connected buildings
        var candidates = buildings.Where(b => connected.ContainsKey(b) && b != current).ToList();
        if (candidates.Count() > 0)
            return candidates.OrderBy(b => Vector2Int.Distance(current, b)).First();

        return buildings.Where(b => b != current).OrderBy(_ => Random.value).First();
    }

    private Vector2Int CreateRoad(Vector2Int start, Vector2Int end, int safetyLimit, Vector2Int startingBuilding, bool isUsingStart = true)
    {
        Vector2Int current = start;
        int safety = 0;

        List<Vector2Int> localRoads = new List<Vector2Int>();
        localRoads.Add(startingBuilding);
        bool firstStep = isUsingStart;

        while (current != end && safety++ < safetyLimit * safetyLimit)
        {
            if (!firstStep)
            {
                if (Mathf.Abs(end.x - current.x) > Mathf.Abs(end.y - current.y))
                    current.x += (end.x > current.x) ? 1 : -1;
                else if (end.y != current.y)
                    current.y += (end.y > current.y) ? 1 : -1;
            }

            if (IsEmpty(current))
            {
                //Debug.Log("627");
                SectionAt(current) = SectionType.rualRoads;
                localRoads.Add(current);
            }

            //step 4  if road is ever adjacent to a building or a road it STOPS
            if (isAdjacentToCell(current, localRoads))
            {
                //Debug.Log("road at " + current + " is adjent to something stopping");
                roadListObjs.Add(new RoadData(localRoads));
                break;
            }
            firstStep = false;
        }

        return current;
    }
    private bool isAdjacentToCell(Vector2Int pos, List<Vector2Int> blacklist)
    {
        foreach (var n in GetNeighbors(pos))
        {
            if (blacklist.Contains(n)) continue;

            if (InBounds(n))
            {
                //Debug.Log("696");
                var type = SectionAt(n);
                if (type != SectionType.Empty && type != SectionType.Outside)
                    return true;
            }
        }
        return false;
    }
    private bool isAdjacentToType(Vector2Int pos, SectionType typeChecked, List<Vector2Int> blacklist)
    {
        foreach (var n in GetNeighbors(pos))
        {
            if (blacklist.Contains(n)) continue;

            if (InBounds(n))
            {
                //Debug.Log("696");
                var type = SectionAt(n);
                if (type == typeChecked)
                    return true;
            }
        }
        return false;
    }
    private bool isAdjacentToType(Vector2Int pos, SectionType typeChecked)
    {
        foreach (var n in GetNeighbors(pos))
        {
            if (InBounds(n))
            {
                //Debug.Log("696");
                var type = SectionAt(n);
                if (type == typeChecked)    
                    return true;
            }
        }
        return false;
    }
    private Vector2Int isAdjacentToCellVector(Vector2Int pos)
    {
        foreach (var n in GetNeighbors(pos))
        {
            if (InBounds(n))
            {
                //Debug.Log("711");
                var type = SectionAt(n);
                if (type != SectionType.Empty && type != SectionType.Outside)
                    return n;
            }
        }
        return Vector2Int.zero;
    }

    private Vector2Int FindNearestOfType(Vector2Int origin, SectionType targetType, int maxDistance = 30)
    {
        if (!InBounds(origin))
            return Vector2Int.zero;

        Queue<Vector2Int> queue = new();
        HashSet<Vector2Int> visited = new();

        queue.Enqueue(origin);
        visited.Add(origin);

        Vector2Int[] dirs = {
        new(1,0), new(-1,0), new(0,1), new(0,-1)    };

        int safety = worldSize * worldSize; // ultimate safety cap

        while (queue.Count > 0 && safety-- > 0)
        {
            Vector2Int current = queue.Dequeue();
            int distance = Mathf.Abs(current.x - origin.x) + Mathf.Abs(current.y - origin.y);

            if (distance > maxDistance)
                continue;

            if (SectionAt(current) == targetType)
                return current;

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;
                if (InBounds(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        Debug.Log($"[FindNearestOfTypeBFS] No {targetType} found near {origin} within {maxDistance} tiles.");
        return Vector2Int.zero;
    }


    private bool IsAdjacentToConnectedTiles(RoadData road, HashSet<Vector2Int> connectedTiles)
    {
        foreach (var vec in road.roads)
        {
            if (connectedTiles.Contains(vec + Vector2Int.up) ||
                connectedTiles.Contains(vec + Vector2Int.down) ||
                connectedTiles.Contains(vec + Vector2Int.left) ||
                connectedTiles.Contains(vec + Vector2Int.right))
                return true;
        }
        return false;
    }

    private Vector2Int FindNearestRoadVector(Vector2Int from, HashSet<Vector2Int> connectedTiles)
    {
        Vector2Int nearest = Vector2Int.zero;
        float minDist = float.MaxValue;

        foreach (var vec in connectedTiles)
        {
            float dist = Vector2Int.Distance(from, vec);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = vec;
            }
        }

        return nearest;
    }

    #region Helpers

    private bool IsType(Vector2Int pos, SectionType type)
    {
        return InBounds(pos) && SectionAt(pos) == type;
    }
    private Vector2Int[] GetNeighbors(Vector2Int pos)
    {
        return new Vector2Int[] { pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.right, pos + Vector2Int.left };
    }
    private CityData GetClosestCity(Vector2Int pos)
    {
        return cityCenters.OrderBy(c => Vector2Int.Distance(pos, c.center)).First();
    }
    private int ToIndex(int coord) => coord + gridOffset;
    private bool InBounds(Vector2Int pos)
    {
        int x = pos.x + gridOffset;
        int y = pos.y + gridOffset;
        return (x >= 0 && x < worldSize && y >= 0 && y < worldSize);
    }
    private ref SectionType SectionAt(Vector2Int pos)
    {
        return ref sectionGrid[pos.x + gridOffset, pos.y + gridOffset];
    }
    private void SetSection(Vector2Int pos, SectionType type)
    {
        if (InBounds(pos))
            sectionGrid[ToIndex(pos.x), ToIndex(pos.y)] = type;
    }
    private bool IsEmpty(Vector2Int pos)
    {
        return InBounds(pos) && sectionGrid[ToIndex(pos.x), ToIndex(pos.y)] == SectionType.Empty;
    }
    private Cube CubeAt(Vector2Int pos)
    {
        return cubeInstances[pos.x + gridOffset, pos.y + gridOffset];
    }
    private void SetCubeAt(Vector2Int pos, Cube cube)
    {
        cubeInstances[pos.x + gridOffset, pos.y + gridOffset] = cube;
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