using UnityEngine;

public enum doorType
{
    road,
    plains,
    water,
    building,
    forest
}

public class cubeDoor : MonoBehaviour
{
    [SerializeField] private doorType doorType;

    [SerializeField] private bool isDefinedConnected;
    [SerializeField] private GameObject PrefabNeeded;
}
