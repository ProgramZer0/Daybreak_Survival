using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class EnemyController : MonoBehaviour
{
    [SerializeField] private GameManager GM;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float spawnRadius = 200f;
    [SerializeField] private int targetEnemyCount = 70;
    [SerializeField] private GroundBuilder planetBuilder;

    private List<GameObject> enemies = new List<GameObject>();


    [SerializeField] private bool spawningEnabled = false;

    public void enableSpawning()
    {
        spawningEnabled = true;
    }

    private void Update()
    {
        if (!spawningEnabled) return;
        enemies = enemies.Where(e => e != null).ToList();

        int nearbyCount = enemies.Count(e =>
            Vector2.Distance(player.position, e.transform.position) <= spawnRadius);

        if (nearbyCount < targetEnemyCount)
        {
            SpawnEnemyNearPlayer();
        }

        foreach (var enemy in enemies.ToList())
        {
            if (enemy == null) continue;
            EnemyBase enemyComp = enemy.GetComponent<EnemyBase>();
            enemyComp?.Tick();
        }
    }

    private void SpawnEnemyNearPlayer()
    {
        var possibleSpawners = planetBuilder.enemySpawnersObj
            .Where(s => Vector2.Distance(player.position, s.transform.position) <= spawnRadius)
            .ToList();

        if (possibleSpawners.Count == 0) return;

        GameObject spawnerObj = possibleSpawners[Random.Range(0, possibleSpawners.Count)];

        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = spawnerObj.GetComponent<Spawner>().Spawn(enemyPrefab, player);
        if (enemy != null)
        {
            enemies.Add(enemy);
        }
    }

    public void SetIsDay(bool isDay)
    {
        foreach (var enemy in enemies.ToList())
        {
            if (enemy == null) continue;
            EnemyBase enemyComp = enemy.GetComponent<EnemyBase>();
            enemyComp.isDay = isDay;
        }
    }
}
