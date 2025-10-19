using System.Collections.Generic;
using UnityEngine;

public class OtherSpawner : MonoBehaviour
{
    [SerializeField] private Weapon[] weapons;
    [SerializeField] private LifeStyles[] LifeStyles;
    [SerializeField] private Ammo[] ammos;
    [SerializeField] private TerrainBuilder TB;

    private List<GameObject> spawnobjs;

    private void Awake()
    {
        spawnobjs = new List<GameObject>();
    }

    public void SpawnAll()
    {
        foreach (GameObject obj in TB.weaponSpawnerobj)
        {
            if (obj == null) continue;

            Weapon weaponInit = GetRandomByRarity(weapons);
            if (weaponInit == null)
                continue;
            GameObject spawn = obj.GetComponent<Spawner>().Spawn(weaponInit.prefab);
            if (spawn != null)
            {
                spawnobjs.Add(spawn);
            }
        }

        foreach (GameObject obj in TB.ammoSpawnerobj)
        {
            if (obj == null) continue;

            Ammo ammoInit = ammos[Random.Range(0, ammos.Length)];
            if (ammoInit == null)
                continue;
            GameObject spawn = obj.GetComponent<Spawner>().Spawn(ammoInit.prefab);

            if (spawn != null)
            {
                spawnobjs.Add(spawn);
            }
        }

        foreach (GameObject obj in TB.lifeStyleSpawnerobj)
        {
            if (obj == null) continue;

            GameObject spawn = obj.GetComponent<Spawner>().Spawn();
            if (spawn != null)
            {
                spawnobjs.Add(spawn);
            }
        }
    }

    public void DespawnAll()
    {
        foreach (GameObject obj in spawnobjs)
            Destroy(obj, 0.5f);
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
