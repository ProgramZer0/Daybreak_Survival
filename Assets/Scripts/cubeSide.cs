using UnityEngine;

public enum SideType
{
    road,
    plains,
    water,
    building
}
public enum SideDirection
{
    Up,
    Down,
    Left,
    Right
}

public class cubeSide : MonoBehaviour
{
    public SideType sideType;
    public SideDirection sideDirection;

    public bool isDefinedConnected;
    public GameObject PrefabNeeded;
}
