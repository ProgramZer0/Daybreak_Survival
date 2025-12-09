using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteMask))]
public class TilemapToMask : MonoBehaviour
{
    public Tilemap tilemap;         
    public int pixelsPerUnit = 100;         
    public int paddingPixels = 2;           
    public Camera renderCameraPrefab;       
    public LayerMask tilemapLayer;          
    public bool destroyCameraAfter = true;  

    private SpriteMask mask;

    void Awake()
    {
        mask = GetComponent<SpriteMask>();
        if (tilemap == null)
        {
            Debug.LogError("TilemapToMask: Assign a Tilemap in inspector.");
            return;
        }

        CreateMaskFromTilemap();
    }

    public void CreateMaskFromTilemap()
    {
        // 1) Get tilemap bounds in world space
        BoundsInt bInt = tilemap.cellBounds;
        if (bInt.size.x == 0 || bInt.size.y == 0)
        {
            Debug.LogError("TilemapToMask: empty tilemap bounds.");
            return;
        }

        // Convert cell bounds to world-space bounds
        Vector3 minWorld = tilemap.CellToWorld(bInt.min);
        Vector3 maxWorld = tilemap.CellToWorld(bInt.max);
        // Note: maxWorld points to the corner of max cell; adjust using cell size
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        maxWorld += new Vector3(cellSize.x, cellSize.y, 0f);

        Rect worldRect = new Rect(minWorld.x, minWorld.y, maxWorld.x - minWorld.x, maxWorld.y - minWorld.y);

        // 2) Compute texture size (pixels) from world rect & PPU
        int texW = Mathf.CeilToInt(worldRect.width * pixelsPerUnit) + paddingPixels * 2;
        int texH = Mathf.CeilToInt(worldRect.height * pixelsPerUnit) + paddingPixels * 2;

        if (texW <= 0 || texH <= 0)
        {
            Debug.LogError("TilemapToMask: computed texture size invalid.");
            return;
        }

        // 3) Create a temporary camera to render ONLY the tilemap
        Camera cam = null;
        bool createdCamera = false;
        if (renderCameraPrefab != null)
        {
            cam = Instantiate(renderCameraPrefab);
            createdCamera = true;
        }
        else
        {
            GameObject go = new GameObject("TilemapMaskRenderCam");
            cam = go.AddComponent<Camera>();
            createdCamera = true;
        }

        // Configure camera
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // transparent
        cam.cullingMask = tilemapLayer;
        cam.transform.position = new Vector3(worldRect.center.x, worldRect.center.y, -10f); // put behind tilemap
        cam.orthographicSize = worldRect.height * 0.5f;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // 4) Render to a RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(texW, texH, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point; // or Bilinear
        cam.targetTexture = rt;

        // Important: adjust camera aspect/viewport to match worldRect width/height
        float camSizeY = worldRect.height * 0.5f;
        cam.orthographicSize = camSizeY;
        // aspect is auto from RenderTexture dimensions

        cam.Render();

        // 5) Read pixels from RenderTexture into Texture2D
        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, texW, texH), 0, 0);
        tex.Apply();
        RenderTexture.active = activeRT;

        // 6) Create sprite from texture
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

        // 7) Assign to mask
        mask.sprite = s;

        // 8) Position mask so it lines up with tilemap
        // Because we captured worldRect -> texture, and used same PPU and centered pivot,
        // set mask position to worldRect.center and remove scale
        transform.position = new Vector3(worldRect.center.x, worldRect.center.y, transform.position.z);
        transform.localScale = Vector3.one;

        // cleanup
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        if (createdCamera && destroyCameraAfter)
            Destroy(cam.gameObject);
    }
}