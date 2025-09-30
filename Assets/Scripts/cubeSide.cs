using UnityEngine;

public enum SideDirection
{
    Up,
    Down,
    Left,
    Right
}

public class cubeSide : MonoBehaviour
{
    public SectionType[] sideType;
    public SideDirection sideDirection;

    public bool isDefinedConnected;
    public GameObject PrefabNeeded;
}
