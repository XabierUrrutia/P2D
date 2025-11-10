using System.Collections;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    [Header("Fog Textures")]
    public Texture2D blackFogTexture;
    public Texture2D visitedFogTexture;

    [Header("Fog Masks")]
    public SpriteRenderer blackFogRenderer;
    public SpriteRenderer visitedFogRenderer;

    [Header("Settings")]
    public int textureResolution = 2500;
    public float worldSize = 350f;
    [Range(0, 255)]
    public int visitedFogAlpha = 180;

    [Header("Visual Scale")]
    public float visualScale = 2f;

    private Color32[] blackFogPixels;
    private Color32[] visitedFogPixels;
    private bool[] permanentRevealedPixels;
    private bool needsApply = false;
    private Vector2 worldScale;
    private Vector2Int pixelScale;

    // Para manejar la posición anterior del jugador
    private Vector2Int lastPlayerPixelPos;
    private bool hasLastPosition = false;

    void Awake()
    {
        InitializeTextures();
        SetupFogSystem();
        AdjustFogScale();
    }

    void Start()
    {
        AdjustFogScale();
    }

    private void InitializeTextures()
    {
        if (blackFogTexture == null)
        {
            blackFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            blackFogTexture.wrapMode = TextureWrapMode.Clamp;
            blackFogTexture.filterMode = FilterMode.Bilinear;
        }

        if (visitedFogTexture == null)
        {
            visitedFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            visitedFogTexture.wrapMode = TextureWrapMode.Clamp;
            visitedFogTexture.filterMode = FilterMode.Bilinear;
        }

        pixelScale = new Vector2Int(textureResolution, textureResolution);
        worldScale = new Vector2(worldSize, worldSize);
    }

    private void SetupFogSystem()
    {
        blackFogPixels = new Color32[pixelScale.x * pixelScale.y];
        visitedFogPixels = new Color32[pixelScale.x * pixelScale.y];
        permanentRevealedPixels = new bool[pixelScale.x * pixelScale.y];

        Color32 blackColor = new Color32(0, 0, 0, 255);
        Color32 visitedColor = new Color32(0, 0, 0, (byte)visitedFogAlpha);

        for (int i = 0; i < blackFogPixels.Length; i++)
        {
            blackFogPixels[i] = blackColor;
            visitedFogPixels[i] = visitedColor;
            permanentRevealedPixels[i] = false;
        }

        ApplyInitialTextures();
        CreateSprites();
    }

    private void AdjustFogScale()
    {
        float scale = visualScale;

        if (blackFogRenderer != null)
        {
            blackFogRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (visitedFogRenderer != null)
        {
            visitedFogRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void ApplyInitialTextures()
    {
        blackFogTexture.SetPixels32(blackFogPixels);
        visitedFogTexture.SetPixels32(visitedFogPixels);
        blackFogTexture.Apply();
        visitedFogTexture.Apply();
    }

    private void CreateSprites()
    {
        if (blackFogRenderer != null)
        {
            Sprite blackSprite = Sprite.Create(blackFogTexture,
                new Rect(0, 0, textureResolution, textureResolution),
                new Vector2(0.5f, 0.5f), 100f);
            blackFogRenderer.sprite = blackSprite;
            blackFogRenderer.sortingOrder = 2;
        }

        if (visitedFogRenderer != null)
        {
            Sprite visitedSprite = Sprite.Create(visitedFogTexture,
                new Rect(0, 0, textureResolution, textureResolution),
                new Vector2(0.5f, 0.5f), 100f);
            visitedFogRenderer.sprite = visitedSprite;
            visitedFogRenderer.sortingOrder = 1;
        }
    }

    public void UpdatePlayerPosition(Vector2 worldPos, float visionRadius)
    {
        Vector2Int pixelPos = WorldToPixel(worldPos);
        int pixelRadius = Mathf.RoundToInt(visionRadius * textureResolution / worldSize);

        // Si tenemos una posición anterior, restaurar la niebla visitada en esa área
        if (hasLastPosition)
        {
            RestoreVisitedFog(lastPlayerPixelPos, pixelRadius);
        }

        // Revelar nueva posición
        RevealArea(pixelPos, pixelRadius, false);

        // Marcar como permanentemente revelada
        RevealArea(pixelPos, pixelRadius, true);

        lastPlayerPixelPos = pixelPos;
        hasLastPosition = true;

        needsApply = true;
    }

    private void RevealArea(Vector2Int center, int radius, bool permanent)
    {
        int radiusSqr = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSqr)
                {
                    int pixelX = center.x + x;
                    int pixelY = center.y + y;

                    if (pixelX >= 0 && pixelX < textureResolution && pixelY >= 0 && pixelY < textureResolution)
                    {
                        int index = pixelY * textureResolution + pixelX;

                        if (permanent)
                        {
                            // Revelación permanente: quitar niebla negra
                            permanentRevealedPixels[index] = true;
                            blackFogPixels[index] = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            // Revelación temporal: área completamente visible
                            visitedFogPixels[index] = new Color32(0, 0, 0, 0);
                            blackFogPixels[index] = new Color32(0, 0, 0, 0);
                        }
                    }
                }
            }
        }
    }

    private void RestoreVisitedFog(Vector2Int oldCenter, int radius)
    {
        int radiusSqr = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSqr)
                {
                    int pixelX = oldCenter.x + x;
                    int pixelY = oldCenter.y + y;

                    if (pixelX >= 0 && pixelX < textureResolution && pixelY >= 0 && pixelY < textureResolution)
                    {
                        int index = pixelY * textureResolution + pixelX;

                        // Restaurar niebla visitada (pero mantener transparente la niebla negra en áreas permanentemente reveladas)
                        if (permanentRevealedPixels[index])
                        {
                            visitedFogPixels[index] = new Color32(0, 0, 0, (byte)visitedFogAlpha);
                        }
                    }
                }
            }
        }
    }

    private Vector2Int WorldToPixel(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - (Vector2)transform.position;
        Vector2 normalizedPos = new Vector2(
            (localPos.x + worldSize * 0.5f) / worldSize,
            (localPos.y + worldSize * 0.5f) / worldSize
        );

        normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
        normalizedPos.y = Mathf.Clamp01(normalizedPos.y);

        return new Vector2Int(
            Mathf.RoundToInt(normalizedPos.x * (textureResolution - 1)),
            Mathf.RoundToInt(normalizedPos.y * (textureResolution - 1))
        );
    }

    private void LateUpdate()
    {
        if (needsApply)
        {
            blackFogTexture.SetPixels32(blackFogPixels);
            visitedFogTexture.SetPixels32(visitedFogPixels);
            blackFogTexture.Apply();
            visitedFogTexture.Apply();
            needsApply = false;
        }
    }
}