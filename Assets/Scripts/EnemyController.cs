using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private GameManager GM;
    [SerializeField] protected SoundManager SM;
    [SerializeField] protected TerrainBuilder TB;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnRadius = 80f;
    [SerializeField] private float spawnRadius = 300f;
    [SerializeField] private int minEnemyCount = 40;
    [SerializeField] private int maxEnemyCount = 200;

    private List<GameObject> enemies = new List<GameObject>();
    [SerializeField] private bool spawningEnabled = false;
    private bool isDay = true;

    public void enableSpawning(bool enabled) { spawningEnabled = enabled; }

    private void Update()
    {
        if (!spawningEnabled) return;

        enemies = enemies.Where(e => e != null).ToList();

        int nearbyCount = enemies.Count(e =>
            Vector2.Distance(player.position, e.transform.position) <= minSpawnRadius);

        if (nearbyCount < minEnemyCount)
        {
            SpawnEnemyNearPlayer();
        }
        else if (enemies.Count > maxEnemyCount)
        {
            DespawnPastPlayer();
        }

        if (enemies.Count == 0) return;

        foreach (GameObject obj in enemies)
        {
            obj.GetComponent<EnemyBase>().Tick();
        }
    }

    private void SpawnEnemyNearPlayer()
    {
        if (enemies.Count >= maxEnemyCount) return; 

        var possibleSpawners = TB.enemySpawnersObj
            .Where(s =>
            {
                float dist = Vector2.Distance(player.position, s.transform.position);
                return dist >= minSpawnRadius && dist <= spawnRadius;
            })
            .ToList();

        if (possibleSpawners.Count == 0) return;

        GameObject spawnerObj = possibleSpawners[Random.Range(0, possibleSpawners.Count)];
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = spawnerObj.GetComponent<Spawner>().Spawn(enemyPrefab, player, isDay, SM);
        if (enemy != null)
        {
            enemies.Add(enemy);
        }
    }
    public void SetIsDay(bool _isDay)
    {
        isDay = _isDay;
        foreach (var enemy in enemies.ToList())
        {
            if (enemy == null) continue;
            EnemyBase enemyComp = enemy.GetComponent<EnemyBase>();
            enemyComp.isDay = _isDay;
        }
    }

    public void DespawnAllEnemies()
    {
        foreach (GameObject obj in enemies)
            Destroy(obj, 0.5f);
    }

    public void DespawnPastPlayer()
    {
        var tooFarEnemies = enemies
            .Where(e => Vector2.Distance(player.position, e.transform.position) > spawnRadius)
            .ToList();

        foreach (GameObject obj in tooFarEnemies)
        {
            Destroy(obj, 0.5f);
            enemies.Remove(obj);
        }
    }
}