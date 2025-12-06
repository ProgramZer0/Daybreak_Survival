using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingLight : MonoBehaviour
{
    [SerializeField] private LayerMask layersImpacted;
    [SerializeField] private SpriteMask mask;
    [SerializeField] private IndoorBlackoutFader blackout;

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
            mask.enabled = true;
            blackout.FadeIn();
        }   
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            GM.ExitingBuilding();
            mask.enabled = false;
            blackout.FadeOut();
        }   
    }

    private bool IsInLayerMask(GameObject obj)
    {
        return (layersImpacted.value & (1 << obj.layer)) != 0;
    }
}
