using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public List<GameObject> spawns;
    [SerializeField] private int maxSpawns = 5;

    private void Update()
    {
        spawns = spawns.Where(e => e != null).ToList();
    }
    private void Awake()
    {
        spawns = new List<GameObject>();
    }
    public GameObject Spawn(GameObject prefab)
    {
        if (spawns.Count >= maxSpawns) return null;

        GameObject o = Instantiate(prefab, transform.position, Quaternion.identity);
        spawns.Add(o);

        IEnemy enemy = o.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(FindFirstObjectByType<PlayerInterface>().gameObject);
        }
        return o;
    }

    public GameObject Spawn(GameObject prefab, Transform player)
    {
        if (spawns.Count >= maxSpawns) return null;

        GameObject o = Instantiate(prefab, transform.position, Quaternion.identity);
        spawns.Add(o);

        IEnemy enemy = o.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(player.gameObject);
        }

        return o;

    }

    public GameObject Spawn(GameObject prefab, Transform player, bool isDay, SoundManager SM)
    {
        if (spawns.Count >= maxSpawns) return null;

        GameObject o = Instantiate(prefab, transform.position, Quaternion.identity);
        spawns.Add(o);

        IEnemy enemy = o.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(player.gameObject, isDay, SM);
        }

        return o;

    }

    public void Despawn(GameObject obj)
    {
        if (spawns.Contains(obj)) spawns.Remove(obj);
        Destroy(obj);
    }
}
