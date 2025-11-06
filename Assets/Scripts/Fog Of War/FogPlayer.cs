using System.Collections;
using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog Settings")]
    public FogOfWar fogOfWar;
    public float visionRadius = 5f;
    public float updateInterval = 0.05f;

    [Header("Secondary Fog")]
    public Transform secondaryFog;
    public float secondaryFogRadius = 8f;

    private Vector3 lastPosition;
    private float moveThreshold = 0.01f;
    private Coroutine fogUpdateCoroutine;

    void Start()
    {
        InitializeFogSystem();
        StartFogUpdates();
    }

    void Update()
    {
        HandleMovement();
        UpdateSecondaryFog();
    }

    private void InitializeFogSystem()
    {
        if (fogOfWar == null)
            fogOfWar = FindObjectOfType<FogOfWar>();

        if (secondaryFog != null)
        {
            secondaryFog.localScale = Vector3.one * secondaryFogRadius * 2f;
        }
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
            UpdateFogReveal();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateFogReveal()
    {
        if (fogOfWar == null) return;

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved > moveThreshold)
        {
            // Revelação normal (não permanente) para área atual
            fogOfWar.RevealArea(transform.position, visionRadius, false);
            lastPosition = transform.position;
        }
    }

    private void UpdateSecondaryFog()
    {
        if (secondaryFog != null)
        {
            secondaryFog.position = transform.position;
        }
    }

    private void HandleMovement()
    {
        // Teu código de movimento existente
        if (Input.GetKey(KeyCode.W))
            transform.position += Vector3.up * Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.S))
            transform.position += Vector3.down * Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.A))
            transform.position += Vector3.left * Time.deltaTime * 5f;
        if (Input.GetKey(KeyCode.D))
            transform.position += Vector3.right * Time.deltaTime * 5f;
    }

    public void RevealPermanentArea(Vector2 position, float radius)
    {
        if (fogOfWar != null)
        {
            fogOfWar.RevealArea(position, radius, true);
        }
    }

    void OnDestroy()
    {
        if (fogUpdateCoroutine != null)
            StopCoroutine(fogUpdateCoroutine);
    }
}