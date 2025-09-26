using UnityEngine;

public enum SideType
{
    road,
    plains,
    water,
    building,
    forest
}

public class cubeSide : MonoBehaviour
{
    public SideType sideType;

    public bool isDefinedConnected;
    public GameObject PrefabNeeded;
}
