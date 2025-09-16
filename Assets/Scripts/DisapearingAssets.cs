using UnityEngine;

public class DisapearingAssets : MonoBehaviour 
{ 
    [SerializeField] private LayerMask layersImpacted;
    [SerializeField] private SpriteRenderer rend;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            Color temp = rend.color;
            temp.a = .7f;
            rend.color = temp;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject))
        {
            Color temp = rend.color;
            temp.a = 1f;
            rend.color = temp;
        }
    }

    private bool IsInLayerMask(GameObject obj)
    {
        return (layersImpacted.value & (1 << obj.layer)) != 0;
    }
}