using UnityEngine;

public class EnemyFogVisibility : MonoBehaviour
{
    [Header("Configuración de Visibilidad")]
    public bool alwaysVisibleInFog = false; // Para debugging
    public float visibilityCheckInterval = 0.1f;

    private FogOfWar fogOfWar;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private MonoBehaviour[] scripts;
    private float lastCheckTime;
    private bool isVisible = true;

    void Start()
    {
        fogOfWar = FindObjectOfType<FogOfWar>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();
        scripts = GetComponents<MonoBehaviour>();

        // Ocultar inicialmente hasta primera verificación
        SetVisibility(false);

        lastCheckTime = Time.time;
    }

    void Update()
    {
        if (alwaysVisibleInFog) return;

        if (Time.time - lastCheckTime >= visibilityCheckInterval)
        {
            CheckVisibility();
            lastCheckTime = Time.time;
        }
    }

    void CheckVisibility()
    {
        if (fogOfWar == null) return;

        bool shouldBeVisible = fogOfWar.IsPositionVisible(transform.position);

        if (shouldBeVisible != isVisible)
        {
            SetVisibility(shouldBeVisible);
        }
    }

    void SetVisibility(bool visible)
    {
        isVisible = visible;

        // Mostrar/ocultar renderers
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
                rend.enabled = visible;
        }

        // Opcional: habilitar/deshabilitar colliders
        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = visible;
        }

        // Opcional: habilitar/deshabilitar scripts específicos
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this && script.enabled)
            {
                // No deshabilitar scripts esenciales como movimiento básico
                // pero sí scripts de IA, ataque, etc.
                if (script.GetType() != typeof(EnemyFogVisibility))
                {
                    script.enabled = visible;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!isVisible && !alwaysVisibleInFog)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}