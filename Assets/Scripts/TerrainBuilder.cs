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

    public CityData(Vector2Int c, int r, CityStyle t)
    {
        center = c;
        radius = r;
        type = t;
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
                Debug.LogWarning($"not in bounds at {neighborPos}");
                neighborTypes[dir] = SectionType.Outside;
                continue;
            }

            neighborTypes[dir] = SectionAt(neighborPos);
        }

        // Step 2: Get prefabs of target section type
        if (!cubeDict.TryGetValue(fallbackType, out var cubeList) || cubeList.Count == 0)
        {
            Debug.LogError($"no cubes found");
            return null;
        }

        List<Cube> candidates = new();

        // Step 3: Check each prefab against neighbor SectionTypes
        foreach (Cube prefab in cubeList)
        {
            bool matches = true;

            if (prefab.cubeType != fallbackType)
                continue;

            foreach (var side in prefab.sides)
            {
                if (!neighborTypes.TryGetValue(side.sideDirection, out var neighborType))
                    continue;

                if (side.sideType.Contains(SectionType.anything))
                {
                    Debug.Log($"contains anything");
                    continue;
                }

                if (!side.sideType.Contains(neighborType))
                {
                    Debug.Log($"cube {prefab} does not match side {side.sideDirection}");
                    matches = false;
                    break;
                }
            }

            if (matches)
                candidates.Add(prefab);
        }
        Cube result;
        // Step 4: Pick one, track city placements
        if (candidates.Count > 0)
        {
            result = candidates[Random.Range(0, candidates.Count)];
        }
        else
        {
            Debug.LogWarning($"getting failback cube at location {gridPos}");
            result = GetRandomCube(fallbackType);
        }

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
                            GenerateGridCity(center, radius, section.cityToRoadChance, section.minRoadSpacing, section.maxRoadSpacing);
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
        ConnectAllCities(new Vector2Int(0 + gridOffset, 0 + gridOffset));
        ConnectAllCities(new Vector2Int(0 - gridOffset, 0 + gridOffset));
        ConnectAllCities(new Vector2Int(0 + gridOffset, 0 - gridOffset));
        ConnectAllCities(Vector2Int.zero);
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
            //if (c.type != CityStyle.Grid) continue;
            
            if(Vector2Int.Distance(closeTo, c.center) < distance)
            {
                distance = Vector2Int.Distance(closeTo, c.center);
                from = c;
            }
        }

        Debug.Log($"city count {cities.Count}, from city {from.center}");

        
        foreach (CityData city in cities)
        {
            if (connectedCities.Contains(city)) continue;
            var to = city;
            int radius = Mathf.Max(from.radius, to.radius);
            CreateCityRoad(from.center, to.center, from.radius, to.radius);

            Debug.Log($"adding city {from.center} to {to.center}");
            
            if(isNearType(city, SectionType.Highway))
                connectedCities.Add(city);
        }
    }

    private bool isNearType(CityData city, SectionType type)
    {
        int radius = city.radius + 1;
        //if any section grid near the city in the city.radius is the section type then we return true otherwise return false.
        for (int i = city.center.x - radius; i < city.center.x + radius; i++)
        {
            for (int j = city.center.y - radius; j < city.center.y + radius; j++)
            {
                if (IsType(new Vector2Int(i, j), type)) return true;
            }
        }
        
        return false;
    }

    private void CreateCityRoad(Vector2Int startCity, Vector2Int endCity, int startRadius, int endRadius)
    {
        highwayRoads.Clear();

        Vector2Int current = startCity;
        int safety = 0, safetyLimit = 5000;
        //highwayRoads.Add(current);

        while (current != endCity && safety++ < safetyLimit)
        {
            Vector2Int delta = endCity - current;

            // Weighted axis decision for smoother paths
            bool moveX = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? Random.value > 0.5f
                : Random.value > 0.7f;

            Vector2Int step = moveX
                ? new Vector2Int(Mathf.Clamp(delta.x, -1, 1), 0)
                : new Vector2Int(0, Mathf.Clamp(delta.y, -1, 1));

            
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
                

            SectionType section = SectionAt(next);

            if (section == SectionType.Empty || section == SectionType.Plains || section == SectionType.rualBuildings) 
            {
                Debug.LogWarning($"setting highway at {next} for city {startCity} to {endCity}");
                SetSection(next, SectionType.Highway); 
            }


            // If we hit a valid road type — we’re done
            bool isRoad = section == SectionType.Road || section == SectionType.rualRoads;
            if (isRoad && Vector2Int.Distance(next, endCity) < endRadius + 2)
            {
                Debug.Log($"hit valid road at {next} for city {startCity} to {endCity}");
                return;
            }

            // Stop if too close to city edge, but not yet on road
            if (isAdjacentToType(next, SectionType.rualBuildings) && Vector2Int.Distance(next, startCity) > startRadius)
            {
                // After stopping, find the nearest road/rural road to connect to
                Vector2Int? nearestRoad = FindNearestRoad(next, 15); // search radius 15 tiles
                if (nearestRoad.HasValue)
                    ConnectToRoad(next, nearestRoad.Value);

                Debug.Log($"Stop if too close to city edge {startCity} , but not yet on road on {next} too close {startRadius} for city {startCity} to {endCity}");
                break;
            }

            highwayRoads.Add(next);
            current = next;
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

    private void GenerateGridCity(Vector2Int center, int radius, float cityChance, int minRoadSpacing, int maxRoadSpacing, float parkPercent = 0.3f)
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
    }

    private void GenerateOrganicCity(Vector2Int center, int radius)
    {
        Debug.Log($"[OrganicCity] center {center}, radius {radius}");

        roadListObjs.Clear();
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
                if (IsEmpty(randomCell))
                {
                    buildings.Add(randomCell);

                    SectionAt(randomCell) = SectionType.rualBuildings;
                    placed++;

                }
            }
        }

        Dictionary<Vector2Int, bool> isConnected = new();

        buildings = buildings.OrderBy(b => Random.value).ToList();

        foreach (var building in buildings)
        {
            //Debug.Log("looking at building " + building);

            //step 1  if building is next to building or a road stop
            Vector2Int adjacentTile = isAdjacentToCellVector(building);
            if (adjacentTile != Vector2Int.zero)
            {
                //Debug.Log("building " + building + " is adjecent to something ");
                roadListObjs.Add(new RoadData(new List<Vector2Int> {building, adjacentTile }));
                continue;
            }

            //step 2 road starts from direction towards center
            Vector2Int startingDir;
            if (building.x > center.x)
            {
                //to the right of center

                if (building.y > center.y)
                {
                    //building is above
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.left;
                    else
                        startingDir = Vector2Int.up;
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
                //to the left of center

                if (building.y > center.y)
                {
                    //building is above
                    if (Random.value >= 0.5f)
                        startingDir = Vector2Int.right;
                    else
                        startingDir = Vector2Int.up;
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

            //step 3 road goes straight towards another building (if there is a building that has a connection we go towards that if not pick random one)
            Vector2Int target = PickTargetBuilding(buildings, building, center, isConnected);

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

        //Step 5 connect any local roads to center
        ConnectAllRoadSegmentsOptimized();
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
            if(!firstStep)
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
        }
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