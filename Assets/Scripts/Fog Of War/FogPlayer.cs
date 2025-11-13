using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public float visionRadius = 5f;

    private FogOfWar fogOfWar;
    private Vector3 lastPosition;

    void Start()
    {
        // Asegurarse de tener tag de Player
        if (gameObject.tag != "Player")
            gameObject.tag = "Player";

        InitializeFogSystem();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Actualizar si nos movimos
        if (fogOfWar != null && Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            fogOfWar.RequestUpdate();
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
        fogOfWar.RegisterPlayer(this);
    }

    // Método para que el FogOfWar se asigne a sí mismo
    public void SetFogOfWar(FogOfWar newFogOfWar)
    {
        fogOfWar = newFogOfWar;
        fogOfWar.RegisterPlayer(this);
    }

    public void SetVisionRadius(float newRadius)
    {
        visionRadius = newRadius;
        if (fogOfWar != null)
        {
            fogOfWar.RequestUpdate();
        }
    }

    void OnDestroy()
    {
        // Desregistrarse del FogOfWar al destruirse
        if (fogOfWar != null)
        {
            fogOfWar.UnregisterPlayer(this);
        }
    }

    void OnEnable()
    {
        // Re-registrarse si se reactiva
        if (fogOfWar != null)
        {
            fogOfWar.RegisterPlayer(this);
            fogOfWar.RequestUpdate();
        }
    }

    void OnDisable()
    {
        // Desregistrarse si se desactiva
        if (fogOfWar != null)
        {
            fogOfWar.UnregisterPlayer(this);
        }
    }
}