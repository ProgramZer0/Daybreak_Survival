using System;
using System.Collections.Generic;
using UnityEngine;

public class GroundBuilder : MonoBehaviour
{
    [SerializeField] private Cube[] cubes;
    [SerializeField] private Cube SpawnCube;
    [SerializeField] private GameObject spawnObj;
    [SerializeField] private int maxCubes = 100;
    [SerializeField] private int depthWantInCubes = 30;

    public LayerMask doorLayer;
    public List<GameObject> weaponSpawnersObjs;
    public List<GameObject> enemySpawnersObj = new List<GameObject>();
    private List<GameObject> cubeObjs;

    private int cubesSpawned = 0;
    private int depth = 0;

    private void Awake()
    {
        cubeObjs = new List<GameObject>();
        weaponSpawnersObjs = new List<GameObject>();

        Debug.Log("[PlanetBuilder] Awake initialized lists.");
    }

    [ContextMenu("generate cubes")]
    public void generateCubes()
    {
        Debug.Log("[PlanetBuilder] Starting cube generation...");
        deleteAllCubes();

        // Start with some seed cubes
        Debug.Log("[PlanetBuilder] Adding initial spawn cube at door 8.");
        addCube(8, SpawnCube);

        System.Random rand = new System.Random();

        // Initialize active cubes list
        List<Cube> activeCubes = new List<Cube>();
        foreach (GameObject go in cubeObjs)
            activeCubes.Add(go.GetComponent<Cube>());

        
        while (depth < depthWantInCubes && cubesSpawned < maxCubes && activeCubes.Count > 0)
        {
            int index = rand.Next(activeCubes.Count);
            Cube creator = activeCubes[index];
            Debug.Log($"[PlanetBuilder] Selected creator cube {creator.name}, depth={depth}, cubesSpawned={cubesSpawned}");

            // Filter doors to exclude 1-3 (upwards)
            List<int> availableDoors = new List<int>();

            Debug.Log($"[PlanetBuilder] Creator {creator.name} has {availableDoors.Count} available doors: {string.Join(",", availableDoors)}");

            if (availableDoors.Count == 0)
            {
                Debug.Log($"[PlanetBuilder] No available doors for cube {creator.name}, removing from active list.");
                activeCubes.RemoveAt(index);
                continue;
            }

            // Pick a random door from the filtered list
            int door = availableDoors[rand.Next(availableDoors.Count)];
            Debug.Log($"[PlanetBuilder] Trying to add cube from creator {creator.name} using door {door}");

            // Spawn a new cube from this door
            Cube newCube = addCube(door, creator);

            if (newCube != null)
            {






                Debug.Log($"[PlanetBuilder] Successfully spawned new cube {newCube.name}, adding to active list.");
                activeCubes.Add(newCube);
            }
            else
            {
                //No cube spawned — keep the door in lists so FinishCube() can wall it later
                Debug.Log($"[PlanetBuilder] Failed to spawn cube from {creator.name} door {door}, leaving door in list for sealing.");
            }
        }

        Debug.Log($"[PlanetBuilder] Generation complete! Spawned {cubesSpawned} cubes, depth: {depth}");

        foreach (GameObject cubeObj in cubeObjs)
        {
            cubeObj.GetComponent<Cube>().FinishCube();
        }

    }

    private void deleteAllCubes()
    {
        Debug.Log("[PlanetBuilder] Deleting all cubes...");
        foreach (GameObject obj in cubeObjs)
            Destroy(obj);

        cubeObjs = new List<GameObject>();
        weaponSpawnersObjs = new List<GameObject>();
        enemySpawnersObj = new List<GameObject>();

        cubesSpawned = 0;
        depth = 0;
    }

    private Vector2 getSpawnLocation(Cube creator, int creatorIDoor, Cube newCube, int newIDoor)
    {
        Debug.Log($"[PlanetBuilder] getSpawnLocation: creator={creator.name}, creatorIDoor={creatorIDoor}, newCube={newCube.name}, newIDoor={newIDoor}");

        if (creatorIDoor < 0 || creatorIDoor >= creator.activeDoors.Count)
        {
            Debug.LogError($"[PlanetBuilder] Invalid creatorIDoor index {creatorIDoor} for cube {creator.name}, activeDoors count={creator.activeDoors.Count}");
            return creator.transform.position;
        }
        if (newIDoor < 0 || newIDoor >= newCube.activeDoors.Count)
        {
            Debug.LogError($"[PlanetBuilder] Invalid newIDoor index {newIDoor} for cube {newCube.name}, activeDoors count={newCube.activeDoors.Count}");
            return creator.transform.position;
        }

        Vector2 creatorDoorWorld = creator.activeDoors[creatorIDoor].transform.position;
        Vector2 newCubeDoorLocal = newCube.activeDoors[newIDoor].transform.localPosition;
        Vector2 spawnPos = creatorDoorWorld - newCubeDoorLocal;
        Debug.Log($"[PlanetBuilder] Calculated spawn position {spawnPos}");
        return spawnPos;
    }

    private Cube addCube(int door, Cube creator)
    {
        Debug.Log($"[PlanetBuilder] addCube called with door={door}, creator={creator.name}");

        int opDoor = findOpDoor(door);
        Debug.Log($"[PlanetBuilder] Opposite door of {door} is {opDoor}");

        // Find all cubes that have a matching door
        List<Cube> compatibleCubes = new List<Cube>();
       

        Debug.Log($"[PlanetBuilder] Found {compatibleCubes.Count} compatible cubes for door {opDoor}");

        if (compatibleCubes.Count == 0)
            return null; // no cube fits, skip

        // Pick one randomly
        Cube selectedCube = compatibleCubes[UnityEngine.Random.Range(0, compatibleCubes.Count)];

        return null;
    }

    private Cube tryToSpawnCube(Cube newCube, int newDoor, Vector2 spawnLocation, Cube creator, int creatorIDoor)
    {
        Debug.Log($"[PlanetBuilder] tryToSpawnCube: newCube={newCube.name}, newDoor={newDoor}, spawnLocation={spawnLocation}, creator={creator.name}, creatorIDoor={creatorIDoor}");

        if (creatorIDoor < 0 || creatorIDoor >= creator.activeDoors.Count)
        {
            Debug.LogError($"[PlanetBuilder] Invalid creatorIDoor index {creatorIDoor} when spawning cube {newCube.name}. Creator {creator.name} has {creator.activeDoors.Count} activeDoors.");
            return null;
        }

        creator.activeDoors[creatorIDoor].SetActive(false);
        Collider2D hit = Physics2D.OverlapCircle(creator.activeDoors[creatorIDoor].transform.position, 0.2f, doorLayer);
        if (hit != null)
        {
            Debug.Log($"[PlanetBuilder] Overlap found, blocked by {hit.gameObject.name}. Cancelling spawn.");
            creator.activeDoors[creatorIDoor].SetActive(true);
            return null;
        }
        creator.activeDoors[creatorIDoor].SetActive(true);

        // Spawn the cube
        GameObject cube = GameObject.Instantiate(newCube.cubePrefab, spawnLocation, Quaternion.identity);
        cube.transform.SetParent(transform, worldPositionStays: true);
        cubesSpawned++;
        cubeObjs.Add(cube);

        Debug.Log($"[PlanetBuilder] Spawned cube {cube.name}, total spawned={cubesSpawned}");

        // Add equipment spawners
        Cube cubeComponent = cube.GetComponent<Cube>();
        if (cubeComponent.weaponSpawners.Length > 0)
        {
            for (int i = 0; i < cubeComponent.weaponSpawners.Length; i++)
            {
                weaponSpawnersObjs.Add(cubeComponent.weaponSpawners[i]);
            }
            Debug.Log($"[PlanetBuilder] Added {cubeComponent.weaponSpawners.Length} equipment spawners from {cube.name}");
        }

        // Add enemy spawners
        if (cubeComponent.enemySpawners.Length > 0)
        {
            for (int i = 0; i < cubeComponent.enemySpawners.Length; i++)
            {
                enemySpawnersObj.Add(cubeComponent.enemySpawners[i]);
            }
            Debug.Log($"[PlanetBuilder] Added {cubeComponent.enemySpawners.Length} enemy spawners from {cube.name}");
        }

        // Increase depth if needed
        if (creatorIDoor >= 7 && creatorIDoor <= 9)
        {
            depth++;
            Debug.Log($"[PlanetBuilder] Increased depth to {depth}");
        }

        return cubeComponent;
    }

    private int findOpDoor(int d)
    {
        switch (d)
        {
            case 1: return 9;
            case 2: return 8;
            case 3: return 7;
            case 4: return 12;
            case 5: return 11;
            case 6: return 10;
            case 7: return 3;
            case 8: return 2;
            case 9: return 1;
            case 10: return 6;
            case 11: return 5;
            case 12: return 4;
        }
        Debug.LogWarning($"[PlanetBuilder] findOpDoor called with unexpected value {d}");
        return 0;
    }
}