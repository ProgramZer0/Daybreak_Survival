using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public List<GameObject> spawns;
    [SerializeField] private int maxSpawns = 5;
    [SerializeField] private float spawnDistance = 0f;
    [SerializeField] private float spawnChance = 1; //1 = 100% 0 = 0% chance
    [SerializeField] private LifeStyles[] lifeStyles;

    private void Update()
    {
        spawns = spawns.Where(e => e != null).ToList();
    }
    private void Awake()
    {
        spawns = new List<GameObject>();
    }
    public GameObject Spawn()
    {
        if (Random.value > spawnChance) return null;
        if (spawns.Count >= maxSpawns) return null;

        if (lifeStyles == null || lifeStyles.Length == 0)
            return null;

        // Pick the LifeStyle to use
        LifeStyles selectedLifeStyle;
        if (lifeStyles.Length == 1)
            selectedLifeStyle = lifeStyles[0];
        else
            selectedLifeStyle = GetRandomByRarity(lifeStyles);

        if (selectedLifeStyle == null || selectedLifeStyle.prefab == null)
            return null;

        // Spawn the prefab
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnDistance;
        GameObject spawnedObj = Instantiate(selectedLifeStyle.prefab, spawnPos, selectedLifeStyle.prefab.transform.rotation);

        spawns.Add(spawnedObj);

        return spawnedObj;
    }

    public GameObject Spawn(GameObject prefab)
    {
        if (Random.value > spawnChance) return null;
        if (spawns.Count >= maxSpawns) return null;
        Vector2 spawnPos = (Vector2)transform.position + (Random.insideUnitCircle * spawnDistance);
        GameObject o = Instantiate(prefab, spawnPos, prefab.transform.rotation);

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
        if (Random.value > spawnChance) return null;
        if (spawns.Count >= maxSpawns) return null;

        Vector2 spawnPos = (Vector2)transform.position + (Random.insideUnitCircle * spawnDistance);
        GameObject o = Instantiate(prefab, spawnPos, prefab.transform.rotation);
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
        if (Random.value > spawnChance) return null;
        if (spawns.Count >= maxSpawns) return null;

        Vector2 spawnPos = (Vector2)transform.position + (Random.insideUnitCircle * spawnDistance);
        GameObject o = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        spawns.Add(o);

        IEnemy enemy = o.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.Initialize(player.gameObject, isDay, SM);
        }

        return o;

    }

    private T GetRandomByRarity<T>(T[] items) where T : ScriptableObject, IRarityItem
    {
        float totalWeight = 0f;

        foreach (var item in items)
            totalWeight += GetRarityWeight(item.Rarity);

        float randomValue = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (var item in items)
        {
            current += GetRarityWeight(item.Rarity);
            if (randomValue <= current)
                return item;
        }

        return items.Length > 0 ? items[0] : null;
    }

    private float GetRarityWeight(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.common: return 50f;
            case Rarity.uncommon: return 25f;
            case Rarity.Rare: return 15f;
            case Rarity.VeryRare: return 8f;
            case Rarity.Impossible: return 2f;
            default: return 1f;
        }
    }
}
