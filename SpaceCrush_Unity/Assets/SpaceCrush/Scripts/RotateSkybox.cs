using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    [SerializeField] private float speed = 0.1f;
    private float rotation = 0.0f;
    private readonly int rotationID = Shader.PropertyToID("_Rotation");

    private void Start()
    {
        rotation = RenderSettings.skybox.GetFloat(rotationID);
    }

    void Update()
    {
        RenderSettings.skybox.SetFloat(rotationID, rotation + (Time.time * speed));
    }

    private void OnDisable()
    {
        RenderSettings.skybox.SetFloat(rotationID, rotation);
    }
}
