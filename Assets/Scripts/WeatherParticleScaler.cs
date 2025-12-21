using UnityEngine;

public class WeatherParticleScaler : MonoBehaviour
{
    public Camera targetCamera;
    public float widthMultiplier = 2f;
    public float heightMultiplier = 2f;

    void LateUpdate()
    {
        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;

        var shape = GetComponent<ParticleSystem>().shape;
        shape.scale = new Vector3(
            width * widthMultiplier,
            height * heightMultiplier,
            1f
        );

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            transform.position.z
        );
    }
}
