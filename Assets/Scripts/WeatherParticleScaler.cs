using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WeatherParticleScaler : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Emitter Size")]
    public float widthMultiplier = 1.2f;
    public float heightMultiplier = 1.2f;

    [Header("Density")]
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

        // Resize the emission box
        shape.scale = new Vector3(width, height, 1f);

        // Offset the emission box so its center is at the top-left of the screen
        //shape.position = new Vector3( -camWidth * 0.5f, camHeight * 0.5f, 0f );

        // Maintain constant density
        float area = width * height;
        emission.rateOverTime = area * density;
    }
}
