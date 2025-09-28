using System.Collections.Generic;
using UnityEngine;

public class GroundBuilder : MonoBehaviour
{
    
    [Header("Prefabs")]
    [SerializeField] private List<Cube> allCubePrefabs;   // drag all cubes here
    [SerializeField] private GameObject roadFallback;     // optional backup prefab
    [SerializeField] private GameObject player;

    [Header("Generation Settings")]
    [SerializeField] private int buildingRange = 300;
    [SerializeField] private int minBuildingSpacing = 60;
    [SerializeField] private int roadSpacing = 2;
    [SerializeField] private int minBuildings = 1;
    [SerializeField] private int maxBuildings = 5;
    [SerializeField] private float cubeSize = 25f;
    [SerializeField] private int chunkSize = 300;
    [SerializeField] private float generationBuffer = 100f;
    [SerializeField] private float branchChance = 0.5f; 

    // Data
    private Dictionary<SideType, List<Cube>> cubeDict;
    private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private List<GameObject> spawnedBuildings = new List<GameObject>();
    private List<GameObject> objThatNeedRoads = new List<GameObject>();
    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();
    private List<GameObject> roads = new List<GameObject>();
    //private Dictionary<Vector2Int, SideDirection> connectionRequirements = new Dictionary<Vector2Int, SideDirection>();
    
    /*
    public bool generationEnabled = false;

    private void Awake()
    {
        BuildDictionary();
        Debug.Log("[GroundBuilder] Awake initialized dictionary.");
    }

    private void Update()
    {
        if(generationEnabled)
            GenerateIfNeeded(player.transform.position);
    }

    private void BuildDictionary()
    {
        cubeDict = new Dictionary<SideType, List<Cube>>();

        foreach (Cube cube in allCubePrefabs)
        {
            if (!cubeDict.ContainsKey(cube.cubeType))
                cubeDict[cube.cubeType] = new List<Cube>();

            cubeDict[cube.cubeType].Add(cube);
        }
    }

    // --- Chunk Generation ---
    public void GenerateIfNeeded(Vector2 playerPos)
    {
        Vector2Int currentChunk = WorldToChunk(playerPos);
        int bufferChunks = Mathf.CeilToInt(generationBuffer / chunkSize);

        for (int x = -bufferChunks; x <= bufferChunks; x++)
        {
            for (int y = -bufferChunks; y <= bufferChunks; y++)
            {
                Vector2Int checkChunk = currentChunk + new Vector2Int(x, y);

                if (!generatedChunks.Contains(checkChunk))
                {
                    GenerateChunk(checkChunk);
                    generatedChunks.Add(checkChunk);
                }
            }
        }
    }

    public void GenerateChunk(Vector2Int chunkCoord)
    {
        Vector2 worldCenter = ChunkToWorld(chunkCoord);
        Debug.Log($"[ChunkGen] Generating chunk {chunkCoord} at {worldCenter}");

        SpawnBuildings(worldCenter);
        GenerateRoads();
        FillEmptyCells(chunkCoord);
    }

    // --- Buildings ---
    private void SpawnBuildings(Vector2 chunkCenter)
    {
        int buildingCount = Random.Range(minBuildings, maxBuildings + 1);
        int attempts = 0;

        while (buildingCount > 0 && attempts < buildingCount * 20)
        {
            attempts++;

            Vector2Int randomCell = WorldToGrid(
                chunkCenter + Random.insideUnitCircle * (chunkSize * 0.5f)
            );

            // spacing check
            bool tooClose = false;
            foreach (Vector2Int existing in occupiedCells)
            {
                if (Vector2Int.Distance(existing, randomCell) < minBuildingSpacing / cubeSize)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            Cube prefab = GetRandomCube(SideType.building);
            if (prefab == null) continue;

            Vector2 spawnPos = GridToWorld(randomCell);
            GameObject newCubeObj = Instantiate(prefab.cubePrefab, spawnPos, Quaternion.identity, transform);
            Cube newCube = newCubeObj.GetComponent<Cube>();

            spawnedBuildings.Add(newCubeObj);
            occupiedCells.Add(randomCell);

            foreach (cubeSide side in newCube.sides)
            {
                if (side.isDefinedConnected)
                {
                    Vector2Int addtoCell = randomCell;
                    switch (side.sideDirection)
                    {
                        case SideDirection.Up:
                            addtoCell = randomCell + Vector2Int.up;
                            break;
                        case SideDirection.Down:
                            addtoCell = randomCell + Vector2Int.down;
                            break;
                        case SideDirection.Left:
                            addtoCell = randomCell + Vector2Int.left;
                            break;
                        case SideDirection.Right:
                            addtoCell = randomCell + Vector2Int.right;
                            break;
                    }
                    
                    Vector2 spawnPos2 = GridToWorld(addtoCell);
                    GameObject newCubeObj2 = Instantiate(side.PrefabNeeded, spawnPos2, Quaternion.identity, transform);
                    Cube newCube2 = newCubeObj2.GetComponent<Cube>();

                    spawnedBuildings.Add(newCubeObj2);
                    occupiedCells.Add(addtoCell);
                }
            }

            Debug.Log($"[Buildings] Spawned {newCube.name} at {randomCell}");

            buildingCount--;
        }
    }

    // --- Roads ---
    private void GenerateRoads()
    {
        Queue<GameObject> frontier = new Queue<GameObject>();

        // add new buildings to the frontier
        foreach (GameObject obj in spawnedBuildings)
        {
            if (!objThatNeedRoads.Contains(obj))
            {
                objThatNeedRoads.Add(obj);
                frontier.Enqueue(obj);
                Debug.Log($"[Roads] Added building {obj.name} to frontier");
            }
        }

        // expand roads until no frontier left
        while (frontier.Count > 0)
        {
            GameObject currentObj = frontier.Dequeue();
            Cube currentCube = currentObj.GetComponent<Cube>();
            Vector2Int currentCell = WorldToGrid(currentObj.transform.position);

            Debug.Log($"[Roads] Expanding from {currentObj.name} at {currentCell}");

            foreach (cubeSide side in currentCube.sides)
            {
                if (side == null || side.sideType != SideType.road) continue;

                Vector2Int dir = DirFromSide(side.sideDirection);
                if (dir == Vector2Int.zero) continue;

                Vector2Int nextCell = currentCell + dir;
                SideDirection needed = OppositeDirection(side.sideDirection);

                Debug.Log($"[Roads] Checking side {side.sideDirection} next cell {nextCell}, needs road at {needed}");

                int steps = 0;
                int maxSteps = 50;

                while (steps < maxSteps)
                {
                    steps++;

                    if (!WithinRange(nextCell))
                    {
                        Debug.Log($"[Roads] Stopped, {nextCell} out of range");
                        break;
                    }

                    if (occupiedCells.Contains(nextCell))
                    {
                        Debug.Log($"[Roads] Stopped, {nextCell} already occupied");
                        break;
                    }

                    Cube roadCube = GetRandomCubeWithRoadAt(needed);
                    if (roadCube == null && roadFallback != null)
                    {
                        Debug.Log($"[Roads] Using fallback road at {nextCell}");
                        GameObject fallback = Instantiate(roadFallback, GridToWorld(nextCell), Quaternion.identity, transform);
                        occupiedCells.Add(nextCell);
                        roadCells.Add(nextCell);
                        objThatNeedRoads.Add(fallback);
                        frontier.Enqueue(fallback);
                        break;
                    }
                    else if (roadCube == null)
                    {
                        Debug.Log($"[Roads] No road cube found for {needed}, stopping");
                        break;
                    }

                    // place road
                    GameObject roadObj = Instantiate(roadCube.cubePrefab, GridToWorld(nextCell), Quaternion.identity, transform);
                    Debug.Log($"[Roads] Placed road {roadObj.name} at {nextCell}");

                    occupiedCells.Add(nextCell);
                    roadCells.Add(nextCell);

                    // add to tracking frontier
                    objThatNeedRoads.Add(roadObj);
                    frontier.Enqueue(roadObj);

                    // move forward
                    nextCell += dir;
                }
            }
        }
    }

    // --- Fill Empty ---
    private void FillEmptyCells(Vector2Int chunkCoord)
    {
        int cellsPerChunk = Mathf.FloorToInt(chunkSize / cubeSize);

        for (int x = 0; x < cellsPerChunk; x++)
        {
            for (int y = 0; y < cellsPerChunk; y++)
            {
                Vector2Int cell = new Vector2Int(
                    chunkCoord.x * cellsPerChunk + x,
                    chunkCoord.y * cellsPerChunk + y
                );

                if (occupiedCells.Contains(cell) || roadCells.Contains(cell))
                    continue;

                SideType fillerType = PickRandomFillerType();
                Cube filler = GetRandomCube(fillerType);
                if (filler == null) continue;

                Instantiate(filler.cubePrefab, GridToWorld(cell), Quaternion.identity, transform);
                occupiedCells.Add(cell);
            }
        }
    }

    // --- Helpers ---
    private Cube GetRandomCube(SideType type)
    {
        if (!cubeDict.ContainsKey(type) || cubeDict[type].Count == 0) return null;
        return cubeDict[type][Random.Range(0, cubeDict[type].Count)];
    }

    private Cube GetRandomCubeWithRoadAt(SideDirection requiredSide)
    {
        List<Cube> candidates = new List<Cube>();
        foreach (var cube in allCubePrefabs)
        {
            if (cube.isBuilding)
                continue;
            foreach (cubeSide side in cube.sides)
            {
                if (side != null && side.sideType == SideType.road && side.sideDirection == requiredSide)
                {
                    candidates.Add(cube);
                    break; // no need to keep checking this cube
                }
            }
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /*private Cube GetRandomCubeWithRoadAt(SideDirection requiredSide)
    {
        List<Cube> candidates = new List<Cube>();
        foreach (var cube in allCubePrefabs)
        {
            foreach (cubeSide side in cube.sides)
            {
                if (side != null && side.sideType == SideType.road && side.sideDirection == requiredSide)
                {
                    candidates.Add(cube);
                    break;
                }
            }
        }
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }
    */
    /*
    private SideType PickRandomFillerType()
    {
        //int roll = Random.Range(0, 100);
        //if (roll < 70) return SideType.plains;
        //if (roll < 90) return SideType.forest;
        return SideType.plains;
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cubeSize),
            Mathf.RoundToInt(worldPos.y / cubeSize)
        );
    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * cubeSize, gridPos.y * cubeSize);
    }

    private Vector2Int WorldToChunk(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize)
        );
    }

    private Vector2 ChunkToWorld(Vector2Int chunkCoord)
    {
        return new Vector2(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize);
    }

    private bool WithinRange(Vector2Int cell)
    {
        return Vector2Int.Distance(Vector2Int.zero, cell) <= buildingRange / cubeSize;
    }

    private bool IsTooCloseToRoad(Vector2Int cell)
    {
        foreach (Vector2Int road in roadCells)
        {
            if (Vector2Int.Distance(road, cell) < roadSpacing)
                return true;
        }
        return false;
    }

    private Vector2Int DirFromSide(SideDirection dir)
    {
        switch (dir)
        {
            case SideDirection.Up: return Vector2Int.up;
            case SideDirection.Down: return Vector2Int.down;
            case SideDirection.Left: return Vector2Int.left;
            case SideDirection.Right: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }

    private SideDirection OppositeDirection(SideDirection dir)
    {
        switch (dir)
        {
            case SideDirection.Up: return SideDirection.Down;
            case SideDirection.Down: return SideDirection.Up;
            case SideDirection.Left: return SideDirection.Right;
            case SideDirection.Right: return SideDirection.Left;
            default: return SideDirection.Up;
        }
    }

    public void ClearWorld()
    {
        // Destroy all spawned buildings
        foreach (GameObject obj in spawnedBuildings)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedBuildings.Clear();

        // Destroy any remaining children (roads, filler, extra cubes, etc.)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Reset tracking structures
        roadCells.Clear();
        occupiedCells.Clear();
        generatedChunks.Clear();

        Debug.Log("[GroundBuilder] World cleared.");
    }*/
}
