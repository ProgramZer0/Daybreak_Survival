using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public List<GameObject> activeDoors;

    public float Rarity;
    public GameObject cubePrefab;
    public GameObject[] weaponSpawners;
    public GameObject[] enemySpawners;
    [SerializeField] private LayerMask doorLayer;

    public void FinishCube()
    {
        Debug.Log($"[Cube:{name}] FinishCube called. Sealing {activeDoors.Count} unused doors...");

        for (int i = 0; i < activeDoors.Count; i++)
        {
            if (activeDoors[i] == null)
            {
                Debug.LogWarning($"[Cube:{name}] Door index {i} is null, skipping.");
                continue;
            }


            activeDoors[i].SetActive(false);
            Collider2D hit = Physics2D.OverlapCircle(activeDoors[i].transform.position, 0.2f, doorLayer);
            if (hit != null)
            {
                Debug.Log($"[Cube:{name}] Door {i} already blocked by {hit.gameObject.name}, leaving wall state as-is.");
                activeDoors[i].SetActive(true);
                continue; 
            }
            activeDoors[i].SetActive(true);
        }

        // Optional: clear doors so you know this cube is finished
        activeDoors.Clear();
    }
}
