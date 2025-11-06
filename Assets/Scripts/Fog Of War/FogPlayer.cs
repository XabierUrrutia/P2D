using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public FogOfWar fogOfWar;
    public float visionRadius = 5f;
    public float updateInterval = 0.01f;

    private Coroutine fogUpdateCoroutine;
    private Vector3 lastPosition;
    private List<Vector3> visitedAreas = new List<Vector3>();

    void Start()
    {
        InitializeFogSystem();
        StartFogUpdates();
        lastPosition = transform.position;

        // Revelar posición inicial
        if (fogOfWar != null)
        {
            fogOfWar.RevealArea(transform.position, visionRadius);
            visitedAreas.Add(transform.position);
        }
    }

    void Update()
    {
        HandleMovement();
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        // NO parentizar el FogOfWar - debe ser independiente
    }

    private void StartFogUpdates()
    {
        if (fogUpdateCoroutine != null)
            StopCoroutine(fogUpdateCoroutine);

        fogUpdateCoroutine = StartCoroutine(FogUpdateRoutine());
    }

    private IEnumerator FogUpdateRoutine()
    {
        while (true)
        {
            if (fogOfWar != null)
            {
                UpdateFog();
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateFog()
    {
        // Revelar posición actual
        fogOfWar.RevealArea(transform.position, visionRadius);

        // Si nos hemos movido significativamente, guardar como área visitada
        if (Vector3.Distance(transform.position, lastPosition) > visionRadius * 0.5f)
        {
            visitedAreas.Add(transform.position);
            lastPosition = transform.position;
        }
    }

    private void HandleMovement()
    {
        // Movimiento con ratón (como tenías originalmente)
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            transform.position = mouseWorldPos;
        }
    }

    // Método para revelar un área específica (para objetivos, edificios, etc.)
    public void RevealPermanentArea(Vector2 position, float radius)
    {
        if (fogOfWar != null)
        {
            fogOfWar.RevealArea(position, radius);
        }
    }

    void OnDestroy()
    {
        if (fogUpdateCoroutine != null)
            StopCoroutine(fogUpdateCoroutine);
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar área de visión
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}