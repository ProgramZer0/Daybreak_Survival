using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingLight : MonoBehaviour
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
            GM.EnteringBuilding();
        }   
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            GM.ExitingBuilding();
        }   
    }

    private bool IsInLayerMask(GameObject obj)
    {
        return (layersImpacted.value & (1 << obj.layer)) != 0;
    }
}
