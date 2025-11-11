using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public FogOfWar fogOfWar;
    public float visionRadius = 5f;

    private Vector3 lastPosition;

    void Start()
    {
        // Asegurarse de tener tag de Player
        if (gameObject.tag != "Player")
            gameObject.tag = "Player";

        InitializeFogSystem();
        lastPosition = transform.position;

        // Registrarse con el FogOfWar
        if (fogOfWar != null)
        {
            fogOfWar.SetPlayer(transform);
        }
    }

    void Update()
    {
        if (fogOfWar != null && Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            fogOfWar.UpdatePlayerPosition(transform.position, visionRadius);
            lastPosition = transform.position;
        }
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        // Si aún no se encuentra, intentar después de un breve delay
        if (fogOfWar == null)
        {
            Invoke("InitializeFogSystem", 0.5f);
            return;
        }

        // Registrarse con el FogOfWar
        fogOfWar.SetPlayer(transform);
    }

    public void SetVisionRadius(float newRadius)
    {
        visionRadius = newRadius;
        if (fogOfWar != null)
        {
            fogOfWar.SetVisionRadius(newRadius);
        }
    }

    void OnDestroy()
    {
        // Limpiar referencia si es necesario
    }
}