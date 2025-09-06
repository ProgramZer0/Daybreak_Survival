using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public List<GameObject> activeDoors;
    public List<int> doorNumbers;
    public float Rarity;
    public GameObject cubePrefab;
    public GameObject[] weaponSpawners;
    public GameObject[] enemySpawners;
    [SerializeField] private GameObject[] walls;
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

            if (i < walls.Length && walls[i] != null)
            {
                walls[i].SetActive(true);
                Debug.Log($"[Cube:{name}] Sealed unused door {i} with wall.");
            }
            else
            {
                Debug.LogWarning($"[Cube:{name}] No wall found for door {i}!");
            }
        }

        // Optional: clear doors so you know this cube is finished
        activeDoors.Clear();
        doorNumbers.Clear();
    }

    public int DoesDoorExist(int door)
    {
        for (int i = 0; i < doorNumbers.Count; i++) 
        {
            Debug.Log("is " + i +" == " + doorNumbers[i]);   
            if (door == doorNumbers[i])
                return i;
        }
        return -1;
    }
}
