using System;
using System.Collections.Generic;
using UnityEngine;

public class GroundBuilder : MonoBehaviour
{
    [SerializeField] private Cube[] cubes;
    [SerializeField] private Cube SpawnCube;
    [SerializeField] private GameObject spawnObj;

    public LayerMask doorLayer;
    public List<GameObject> weaponSpawnersObjs;
    public List<GameObject> enemySpawnersObj = new List<GameObject>();
    private List<GameObject> cubeObjs;

    [SerializeField] private int buildingRange = 300;
    [SerializeField] private int minBuildingSpacing = 60;
    [SerializeField] private int roadSpacing = 2;
    [SerializeField] private int minBuildings = 1;
    [SerializeField] private int maxBuildings = 5;
    [SerializeField] private float cubeSize = 25f;
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject[] plainsPrefabs;
    [SerializeField] private GameObject[] forestPrefabs;
    [SerializeField] private GameObject[] waterPrefabs;

    [SerializeField] private int chunkSize = 200; 
    [SerializeField] private float generationBuffer = 100f;

    private Dictionary<SideType, GameObject[]> fillerPrefabs;
    private HashSet<Vector2Int> roadCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private List<GameObject> spawnedBuildings = new List<GameObject>();

    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();

    private void Awake()
    {
        cubeObjs = new List<GameObject>();
        weaponSpawnersObjs = new List<GameObject>();

        Debug.Log("[GroundBuilder] Awake initialized lists.");

        fillerPrefabs = new Dictionary<SideType, GameObject[]> {
            { SideType.plains, plainsPrefabs },
            { SideType.forest, forestPrefabs },
            { SideType.water,  waterPrefabs }
        };
    }

    // Called every frame or on player movement
    public void GenerateIfNeeded(Vector2 playerPos)
    {
        Vector2Int currentChunk = WorldToChunk(playerPos);

        // How many chunks around the player to generate
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

    private void GenerateChunk(Vector2Int chunkCoord)
    {
        Vector2 worldCenter = ChunkToWorld(chunkCoord);

        Debug.Log($"[ChunkGen] Generating chunk {chunkCoord} at world {worldCenter}");

        // Each chunk runs its own generation
        SpawnBuildings(worldCenter);
        GenerateRoads();
        FillEmptyCells(chunkCoord);
    }

    private void SpawnBuildings(Vector2 chunkCenter)
    {
        int buildingCount = UnityEngine.Random.Range(minBuildings, maxBuildings + 1);
        int attempts = 0;

        while (buildingCount > 0 && attempts < buildingCount * 20)
        {
            attempts++;

            Vector2Int randomCell = WorldToGrid(
                chunkCenter + UnityEngine.Random.insideUnitCircle * (chunkSize * 0.5f)
            );

            // Skip if occupied
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

            Cube prefab = PickRandomBuildingCube();
            if (prefab == null) continue;

            Vector2 spawnPos = GridToWorld(randomCell);
            GameObject newCubeObj = Instantiate(prefab.cubePrefab, spawnPos, Quaternion.identity, transform);
            Cube newCube = newCubeObj.GetComponent<Cube>();
            spawnedBuildings.Add(newCubeObj);
            occupiedCells.Add(randomCell);

            Debug.Log($"[Buildings] Spawned {newCube.name} at {randomCell}");

            // Check sides for extra prefabs
            foreach (cubeSide side in newCube.sides)
            {
                if (side == null) continue;
                if (side.isDefinedConnected && side.PrefabNeeded != null)
                {
                    Vector3 attachPos = side.transform.position;
                    Instantiate(side.PrefabNeeded, attachPos, Quaternion.identity, newCube.transform);
                }
            }

            buildingCount--;
        }
    }

    private void GenerateRoads()
    {
        foreach (GameObject buildingObj in spawnedBuildings)
        {
            Cube building = buildingObj.GetComponent<Cube>();
            Vector2Int buildingCell = WorldToGrid(buildingObj.transform.position);

            foreach (cubeSide side in building.sides)
            {
                if (side == null || side.sideType != SideType.road) continue;

                Vector2Int dir = SideToDir(side.transform.localPosition);
                if (dir == Vector2Int.zero) continue;

                Vector2Int current = buildingCell + dir;
                int steps = 0;
                while (WithinRange(current) && steps < buildingRange / cubeSize)
                {
                    if (IsTooCloseToRoad(current)) break;

                    if (occupiedCells.Contains(current))
                    {
                        Debug.Log($"[Roads] Connected road at {current}");
                        break;
                    }

                    Vector2 worldPos = GridToWorld(current);
                    Instantiate(roadPrefab, worldPos, Quaternion.identity, transform);
                    roadCells.Add(current);

                    steps++;
                    current += dir;
                }
            }
        }
    }

    private void FillEmptyCells(Vector2Int chunkCoord)
    {
        int cellsPerChunk = Mathf.CeilToInt(chunkSize / cubeSize);

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
                if (!fillerPrefabs.ContainsKey(fillerType) || fillerPrefabs[fillerType].Length == 0)
                    continue;

                GameObject chosen = fillerPrefabs[fillerType][UnityEngine.Random.Range(0, fillerPrefabs[fillerType].Length)];
                Vector2 worldPos = GridToWorld(cell);
                Instantiate(chosen, worldPos, Quaternion.identity, transform);

                occupiedCells.Add(cell);
            }
        }
    }

    // --- Helpers ---
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

    private Vector2Int SideToDir(Vector3 localPos)
    {
        if (Mathf.Abs(localPos.x) > Mathf.Abs(localPos.y))
            return (localPos.x > 0) ? Vector2Int.right : Vector2Int.left;
        else
            return (localPos.y > 0) ? Vector2Int.up : Vector2Int.down;
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

    private Cube PickRandomBuildingCube()
    {
        List<Cube> buildingCubes = new List<Cube>();
        foreach (Cube c in cubes)
        {
            if (c.isBuilding) buildingCubes.Add(c);
        }
        if (buildingCubes.Count == 0) return null;
        return buildingCubes[UnityEngine.Random.Range(0, buildingCubes.Count)];
    }

    private SideType PickRandomFillerType()
    {
        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < 70) return SideType.plains;
        if (roll < 90) return SideType.forest;
        return SideType.water;
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

}