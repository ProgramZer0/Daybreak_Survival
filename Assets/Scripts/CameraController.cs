using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float scrollSpeed = 1f;
    public float maxScrollOut = 10;
    public float minScrollOut = 3;
    private Vector2 scrollData;

    private void Update()
    {
        scrollData = Input.mouseScrollDelta;

        if (GetComponent<Camera>().orthographicSize > maxScrollOut)
            GetComponent<Camera>().orthographicSize = maxScrollOut;
        if (GetComponent<Camera>().orthographicSize < minScrollOut)
            GetComponent<Camera>().orthographicSize = minScrollOut;

            if (scrollData.y > 0)
            {
                if (GetComponent<Camera>().orthographicSize > minScrollOut)
                    GetComponent<Camera>().orthographicSize = GetComponent<Camera>().orthographicSize - scrollSpeed;
            }
        if (scrollData.y < 0)
        {
            if (GetComponent<Camera>().orthographicSize < maxScrollOut)
                GetComponent<Camera>().orthographicSize = GetComponent<Camera>().orthographicSize + scrollSpeed;
        }

    }
}
