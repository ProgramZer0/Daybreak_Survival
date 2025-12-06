using UnityEngine;

[RequireComponent(typeof(SpriteMask), typeof(PolygonCollider2D))]
public class ColliderMaskShaper : MonoBehaviour
{
    private SpriteMask mask;
    private PolygonCollider2D poly;

    private void Awake()
    {
        mask = GetComponent<SpriteMask>();
        poly = GetComponent<PolygonCollider2D>();

        GenerateMaskSprite();
    }

    private void GenerateMaskSprite()
    {
        Bounds b = poly.bounds;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        Sprite s = Sprite.Create(tex,
                                 new Rect(0, 0, 1, 1),
                                 new Vector2(0.5f, 0.5f),
                                 1f);

        mask.sprite = s;

        transform.localScale = new Vector3(b.size.x, b.size.y, 1f);

        transform.position = b.center;
    }
}
