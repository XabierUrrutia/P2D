using System.Collections;
using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public FogOfWar fogOfWar;
    public float visionRadius = 5f; // Este valor NO cambia
    public float updateInterval = 0.033f;

    private Coroutine fogUpdateCoroutine;
    private Vector3 lastPosition;

    void Start()
    {
        InitializeFogSystem();
        StartFogUpdates();
        lastPosition = transform.position;
        
        if (fogOfWar != null)
        {
            fogOfWar.UpdatePlayerPosition(transform.position, visionRadius);
        }
    }

    void Update()
    {
        lastPosition = transform.position;
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();
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
                fogOfWar.UpdatePlayerPosition(transform.position, visionRadius);
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void OnDestroy()
    {
        if (fogUpdateCoroutine != null)
            StopCoroutine(fogUpdateCoroutine);
    }
}