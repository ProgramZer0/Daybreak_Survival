using UnityEngine;
using UnityEngine.Tilemaps;

public class disapearTiles : MonoBehaviour
{
    [SerializeField] private LayerMask layersImpacted;
    private GameManager GM;

    private void Awake()
    {
        GM = FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            Color temp = GetComponent<Tilemap>().color;
            temp.a = .1f;
            GetComponent<Tilemap>().color = temp;
            GM.inBuilding = true;
        }   
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            if(!GM.inBuilding)
                GM.inBuilding = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            Color temp = GetComponent<Tilemap>().color;
            temp.a = 1f;
            GetComponent<Tilemap>().color = temp;
            GM.inBuilding = false;
        }   
    }

    private bool IsInLayerMask(GameObject obj)
    {
        return (layersImpacted.value & (1 << obj.layer)) != 0;
    }
}
