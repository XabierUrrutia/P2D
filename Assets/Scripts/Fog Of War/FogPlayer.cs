using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogPlayer : MonoBehaviour
{
    [Header("Fog of War Settings")]
    public FogOfWar fogOfWar;
    public Transform secondaryFogOfWar;
    [Range(0, 5)]
    public float sightDistance = 3f;
    public float checkInterval = 0.08f; // Intervalo mais curto para mais suavidade

    [Header("Secondary Fog Settings")]
    public float secondarySightDistance = 6f;

    // Cache para performance
    private Vector3 lastPosition;
    private float squaredMoveThreshold = 0.1f;
    private Coroutine fogCoroutine;

    void Start()
    {
        StartFogUpdate();
        if (secondaryFogOfWar != null)
        {
            secondaryFogOfWar.localScale = new Vector2(secondarySightDistance, secondarySightDistance) * 2f;
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        // Movimento do jogador
        if (Input.GetKey(KeyCode.W))
            transform.position += transform.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            transform.position -= transform.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            transform.position -= transform.right * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            transform.position += transform.right * Time.deltaTime;

        // Atualizar posição do secondary fog
        if (secondaryFogOfWar != null)
        {
            secondaryFogOfWar.position = transform.position;
        }
    }

    private void StartFogUpdate()
    {
        if (fogCoroutine != null)
            StopCoroutine(fogCoroutine);

        fogCoroutine = StartCoroutine(FogUpdateLoop());
    }

    private IEnumerator FogUpdateLoop()
    {
        while (true)
        {
            UpdateFogOfWar();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void UpdateFogOfWar()
    {
        if (fogOfWar == null) return;

        // Só atualiza se o jogador se moveu significativamente
        float sqrDist = (transform.position - lastPosition).sqrMagnitude;
        if (sqrDist > squaredMoveThreshold)
        {
            fogOfWar.MakeHole(transform.position, sightDistance);
            lastPosition = transform.position;
        }
    }

    // Método para definir os limites do nível (áreas sempre visíveis)
    public void SetLevelBounds(Rect bounds)
    {
        if (fogOfWar != null)
        {
            fogOfWar.ClearPermanentFog(bounds);
        }
    }

    private void OnDestroy()
    {
        if (fogCoroutine != null)
        {
            StopCoroutine(fogCoroutine);
        }
    }
}