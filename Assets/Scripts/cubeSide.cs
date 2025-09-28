using UnityEngine;

public enum SideType
{
    None, Road, Building, Plains, Outside, Shack
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
