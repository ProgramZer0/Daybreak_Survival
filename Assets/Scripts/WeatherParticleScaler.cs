using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WeatherParticleScaler : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Emitter Size")]
    public float widthMultiplier = 1.2f;
    public float heightMultiplier = 1.2f;

    [Header("Density")]
    [Tooltip("Particles per world unit squared per second")]
    public float density = 2.5f;

    private ParticleSystem ps;
    private ParticleSystem.ShapeModule shape;
    private ParticleSystem.EmissionModule emission;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        shape = ps.shape;
        emission = ps.emission;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        float width = camWidth * widthMultiplier;
        float height = camHeight * heightMultiplier;

        // Resize emitter
        shape.scale = new Vector3(width, height, 1f);

        // Keep centered on camera
        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            transform.position.z
        );

        // Scale emission by area
        float area = width * height;
        emission.rateOverTime = Mathf.Min(area * density, 20000f);
    }
}
