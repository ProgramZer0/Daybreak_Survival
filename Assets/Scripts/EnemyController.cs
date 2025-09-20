using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class EnemyController : MonoBehaviour
{
    [SerializeField] private GameManager GM;
    [SerializeField] protected SoundManager SM;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float spawnRadius = 100f;
    [SerializeField] private int targetEnemyCount = 200;
    [SerializeField] private GroundBuilder planetBuilder;

    private List<GameObject> enemies = new List<GameObject>();

    [SerializeField] private bool spawningEnabled = false;
    private bool isDay = true;

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

        if (enemies.Count == 0) return;

        foreach(GameObject obj in enemies)
        {
            obj.GetComponent<EnemyBase>().Tick();
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
}
