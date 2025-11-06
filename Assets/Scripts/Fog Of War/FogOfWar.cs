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
    public int textureResolution = 512;
    public float worldSize = 100f;

    private Color32[] blackFogPixels;
    private Color32[] visitedFogPixels;
    private bool needsApply = false;
    private Vector2 worldScale;
    private Vector2Int pixelScale;

    void Awake()
    {
        InitializeTextures();
        SetupFogSystem();
    }

    private void InitializeTextures()
    {
        // Criar texturas se não existirem
        if (blackFogTexture == null)
        {
            blackFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            blackFogTexture.wrapMode = TextureWrapMode.Clamp;
            blackFogTexture.filterMode = FilterMode.Trilinear;
        }

        if (visitedFogTexture == null)
        {
            visitedFogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            visitedFogTexture.wrapMode = TextureWrapMode.Clamp;
            visitedFogTexture.filterMode = FilterMode.Trilinear;
        }

        pixelScale = new Vector2Int(textureResolution, textureResolution);
        worldScale = new Vector2(worldSize, worldSize);
    }

    private void SetupFogSystem()
    {
        // Inicializar arrays de pixels
        blackFogPixels = new Color32[pixelScale.x * pixelScale.y];
        visitedFogPixels = new Color32[pixelScale.x * pixelScale.y];

        // Cores iniciais
        Color32 blackColor = new Color32(0, 0, 0, 255);
        Color32 visitedColor = new Color32(0, 0, 0, 255);

        // Preencher texturas
        for (int i = 0; i < blackFogPixels.Length; i++)
        {
            blackFogPixels[i] = blackColor;
            visitedFogPixels[i] = visitedColor;
        }

        ApplyInitialTextures();
        CreateSprites();
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
        }

        if (visitedFogRenderer != null)
        {
            Sprite visitedSprite = Sprite.Create(visitedFogTexture,
                new Rect(0, 0, textureResolution, textureResolution),
                new Vector2(0.5f, 0.5f), 100f);
            visitedFogRenderer.sprite = visitedSprite;
        }
    }

    public void RevealArea(Vector2 worldPos, float radius, bool permanentReveal = false)
    {
        Vector2Int pixelPos = WorldToPixel(worldPos);
        int pixelRadius = Mathf.RoundToInt(radius * textureResolution / worldSize);

        // Sempre revelar no fog visitado
        UpdateFogArea(visitedFogPixels, pixelPos, pixelRadius, new Color32(0, 0, 0, 0));

        // Se for revelação permanente, também revelar no fog preto
        if (permanentReveal)
        {
            UpdateFogArea(blackFogPixels, pixelPos, pixelRadius, new Color32(0, 0, 0, 0));
        }

        needsApply = true;
    }

    private Vector2Int WorldToPixel(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - (Vector2)transform.position;
        Vector2 normalizedPos = new Vector2(
            (localPos.x + worldSize * 0.5f) / worldSize,
            (localPos.y + worldSize * 0.5f) / worldSize
        );

        return new Vector2Int(
            Mathf.RoundToInt(normalizedPos.x * (textureResolution - 1)),
            Mathf.RoundToInt(normalizedPos.y * (textureResolution - 1))
        );
    }

    private void UpdateFogArea(Color32[] pixels, Vector2Int center, int radius, Color32 clearColor)
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
                        pixels[index] = clearColor;
                    }
                }
            }
        }
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

    public void ClearLevelBounds(Rect bounds)
    {
        Vector2Int min = WorldToPixel(bounds.min);
        Vector2Int max = WorldToPixel(bounds.max);

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                if (x >= 0 && x < textureResolution && y >= 0 && y < textureResolution)
                {
                    int index = y * textureResolution + x;
                    blackFogPixels[index] = new Color32(0, 0, 0, 0);
                    visitedFogPixels[index] = new Color32(0, 0, 0, 0);
                }
            }
        }
        needsApply = true;
    }
}