using UnityEngine;

[RequireComponent(typeof(SpriteMask), typeof(PolygonCollider2D))]
public class ColliderMaskShaper : MonoBehaviour
{
    private SpriteMask mask;
    private PolygonCollider2D poly;

    public int textureSize = 256; // Higher = smoother edges

    private void Awake()
    {
        mask = GetComponent<SpriteMask>();
        poly = GetComponent<PolygonCollider2D>();

        CreateMaskTexture();
    }

    void CreateMaskTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Fill transparent
        Color32[] blank = new Color32[textureSize * textureSize];
        for (int i = 0; i < blank.Length; i++) blank[i] = new Color32(0, 0, 0, 0);
        tex.SetPixels32(blank);

        // Get polygon in world space
        Vector2[] points = poly.points;

        // Convert to pixel space
        Vector2 polyMin = poly.bounds.min;
        Vector2 polySize = poly.bounds.size;

        Vector2[] pixelPoints = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 worldPoint = poly.transform.TransformPoint(points[i]);
            float px = (worldPoint.x - polyMin.x) / polySize.x * textureSize;
            float py = (worldPoint.y - polyMin.y) / polySize.y * textureSize;
            pixelPoints[i] = new Vector2(px, py);
        }

        // Fill polygon (simple scanline fill)
        FillPolygon(tex, pixelPoints, Color.white);

        tex.Apply();

        Sprite s = Sprite.Create(
            tex,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize / polySize.x
        );

        mask.sprite = s;

        // Position & scale mask to fit collider
        transform.position = poly.bounds.center;
    }

    // Basic polygon fill
    void FillPolygon(Texture2D tex, Vector2[] poly, Color color)
    {
        int width = tex.width;
        int height = tex.height;

        // Loop through every pixel
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (PointInPolygon(new Vector2(x, y), poly))
                    tex.SetPixel(x, y, color);
            }
        }
    }

    // Raycast point-in-polygon
    bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) /
                       (poly[j].y - poly[i].y) + poly[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }
}
