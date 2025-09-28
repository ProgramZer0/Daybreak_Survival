using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public List<cubeSide> sides;

    public bool isBuilding = false;
    public float Rarity;
    public GameObject cubePrefab;
    public SectionType cubeType;
    public GameObject[] weaponSpawners;
    public GameObject[] enemySpawners;
    [SerializeField] private LayerMask doorLayer;
    

}
