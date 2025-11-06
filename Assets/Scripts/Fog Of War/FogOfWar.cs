using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    [Header("Primary Fog (Never Visited)")]
    public Texture2D blackFogTexture;
    public SpriteMask blackFogMask;

    [Header("Secondary Fog (Visited Areas)")]
    public Texture2D visitedFogTexture;
    public SpriteMask visitedFogMask;

    [Header("Performance Settings")]
    public float updateInterval = 0.1f;
    public int textureResolution = 512; // Reduzir resolução para performance

    private Vector2 worldScale;
    private Vector2Int pixelScale;
    private Color32[] blackFogPixels;
    private Color32[] visitedFogPixels;
    private bool needsApply = false;
    private Coroutine updateCoroutine;

    public void Awake()
    {
        // Inicializar texturas com resolução otimizada
        InitializeTextures();

        pixelScale.x = textureResolution;
        pixelScale.y = textureResolution;
        worldScale.x = pixelScale.x / 100f * transform.localScale.x;
        worldScale.y = pixelScale.y / 100f * transform.localScale.y;

        // Inicializar arrays de pixels para acesso rápido
        blackFogPixels = blackFogTexture.GetPixels32();
        visitedFogPixels = visitedFogTexture.GetPixels32();

        // Preencher com cores iniciais
        InitializeFogColors();

        CreateSprites();
    }

    private void InitializeTextures()
    {
        // Criar texturas se não existirem
        if (blackFogTexture == null)
        {
            blackFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            blackFogTexture.wrapMode = TextureWrapMode.Clamp;
        }

        if (visitedFogTexture == null)
        {
            visitedFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            visitedFogTexture.wrapMode = TextureWrapMode.Clamp;
        }
    }

    private void InitializeFogColors()
    {
        // Fog preto - totalmente opaco para áreas nunca visitadas
        Color32 blackColor = new Color32(0, 0, 0, 255);
        // Fog visitado - cinzento semi-transparente
        Color32 visitedColor = new Color32(50, 50, 50, 180);

        for (int i = 0; i < blackFogPixels.Length; i++)
        {
            blackFogPixels[i] = blackColor;
            visitedFogPixels[i] = visitedColor;
        }

        blackFogTexture.SetPixels32(blackFogPixels);
        visitedFogTexture.SetPixels32(visitedFogPixels);
        blackFogTexture.Apply();
        visitedFogTexture.Apply();
    }

    private void CreateSprites()
    {
        if (blackFogMask != null)
        {
            blackFogMask.sprite = Sprite.Create(blackFogTexture,
                new Rect(0, 0, blackFogTexture.width, blackFogTexture.height),
                Vector2.one * 0.5f, 100);
        }

        if (visitedFogMask != null)
        {
            visitedFogMask.sprite = Sprite.Create(visitedFogTexture,
                new Rect(0, 0, visitedFogTexture.width, visitedFogTexture.height),
                Vector2.one * 0.5f, 100);
        }
    }

    private Vector2Int WorldToPixel(Vector2 position)
    {
        Vector2Int pixelPosition = Vector2Int.zero;

        float dx = position.x - transform.position.x;
        float dy = position.y - transform.position.y;

        pixelPosition.x = Mathf.RoundToInt(0.5f * pixelScale.x + dx * (pixelScale.x / worldScale.x));
        pixelPosition.y = Mathf.RoundToInt(0.5f * pixelScale.y + dy * (pixelScale.y / worldScale.y));

        return pixelPosition;
    }

    public void MakeHole(Vector2 position, float holeRadius)
    {
        Vector2Int pixelPosition = WorldToPixel(position);
        int radius = Mathf.RoundToInt(holeRadius * pixelScale.x / worldScale.x);

        // Revelar no fog visitado (área atual de visão)
        UpdateFogArea(visitedFogPixels, pixelPosition, radius, new Color32(0, 0, 0, 0));

        // Revelar permanentemente no fog preto (área visitada)
        UpdateFogArea(blackFogPixels, pixelPosition, radius, new Color32(0, 0, 0, 0));

        needsApply = true;
    }

    private void UpdateFogArea(Color32[] pixels, Vector2Int center, int radius, Color32 clearColor)
    {
        int xMin = Mathf.Clamp(center.x - radius, 0, pixelScale.x - 1);
        int xMax = Mathf.Clamp(center.x + radius, 0, pixelScale.x - 1);
        int yMin = Mathf.Clamp(center.y - radius, 0, pixelScale.y - 1);
        int yMax = Mathf.Clamp(center.y + radius, 0, pixelScale.y - 1);

        int radiusSqr = radius * radius;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - center.x;
                int dy = y - center.y;
                int distSqr = dx * dx + dy * dy;

                if (distSqr <= radiusSqr)
                {
                    int index = y * pixelScale.x + x;
                    pixels[index] = clearColor;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (needsApply)
        {
            ApplyTextureChanges();
            needsApply = false;
        }
    }

    private void ApplyTextureChanges()
    {
        visitedFogTexture.SetPixels32(visitedFogPixels);
        blackFogTexture.SetPixels32(blackFogPixels);
        visitedFogTexture.Apply(false);
        blackFogTexture.Apply(false);
    }

    // Método para limpar fog permanentemente numa área (para limites do nível)
    public void ClearPermanentFog(Rect area)
    {
        Vector2Int min = WorldToPixel(area.min);
        Vector2Int max = WorldToPixel(area.max);

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                if (x >= 0 && x < pixelScale.x && y >= 0 && y < pixelScale.y)
                {
                    int index = y * pixelScale.x + x;
                    blackFogPixels[index] = new Color32(0, 0, 0, 0);
                }
            }
        }

        needsApply = true;
    }

    private void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}