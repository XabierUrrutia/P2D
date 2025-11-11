using UnityEngine;
using System.Collections.Generic;

public class FogOfWar : MonoBehaviour
{
    [Header("Configuración Isométrica")]
    public string playerTag = "Player"; // Tag para buscar al jugador
    public float visionRadius = 5f;
    public Vector2 isometricMapSize = new Vector2(100f, 100f);

    [Header("Renderers")]
    public SpriteRenderer blackFogRenderer;
    public SpriteRenderer visionRenderer;

    [Header("Texturas")]
    public int textureSize = 1024;

    private Transform player;
    private Texture2D blackFogTexture;
    private Texture2D visionTexture;
    private Color32[] blackFogPixels;
    private Color32[] visionPixels;
    private bool[] revealedPixels;

    private Vector3 lastPlayerPos;
    private bool playerFound = false;

    void Start()
    {
        InitializeTextures();
        ScaleForIsometric();
        FindPlayer();
    }

    void Update()
    {
        // Si no hemos encontrado al jugador, intentar encontrarlo
        if (!playerFound)
        {
            FindPlayer();
            return;
        }

        // Actualizar si el jugador se movió
        if (player != null && Vector3.Distance(player.position, lastPlayerPos) > 0.05f)
        {
            UpdateFogOfWar();
            lastPlayerPos = player.position;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerFound = true;
            lastPlayerPos = player.position;
            Debug.Log($"Jugador encontrado: {player.name}");

            // Actualizar inmediatamente
            UpdateFogOfWar();
        }
    }

    // Método público para que otros scripts asignen el jugador
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        playerFound = true;
        lastPlayerPos = player.position;
        UpdateFogOfWar();
    }

    public void UpdatePlayerPosition(Vector3 position, float radius)
    {
        // Si no tenemos jugador, intentar encontrarlo
        if (!playerFound)
        {
            FindPlayer();
            return;
        }

        visionRadius = radius;
        UpdateFogOfWar();
    }

    void InitializeTextures()
    {
        blackFogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        visionTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        blackFogTexture.wrapMode = TextureWrapMode.Clamp;
        visionTexture.wrapMode = TextureWrapMode.Clamp;

        blackFogTexture.filterMode = FilterMode.Bilinear;
        visionTexture.filterMode = FilterMode.Bilinear;

        blackFogPixels = new Color32[textureSize * textureSize];
        visionPixels = new Color32[textureSize * textureSize];
        revealedPixels = new bool[textureSize * textureSize];

        // Inicializar
        Color32 black = new Color32(0, 0, 0, 255);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int i = 0; i < blackFogPixels.Length; i++)
        {
            blackFogPixels[i] = black;
            visionPixels[i] = transparent;
            revealedPixels[i] = false;
        }

        ApplyTextures();
        CreateSprites();
    }

    void UpdateFogOfWar()
    {
        if (!playerFound || player == null) return;

        ClearVisionTexture();

        Vector2Int playerPixel = IsometricWorldToPixel(player.position);
        int pixelRadius = Mathf.RoundToInt(visionRadius * textureSize / Mathf.Max(isometricMapSize.x, isometricMapSize.y));

        DrawVisionCircle(playerPixel, pixelRadius);
        UpdatePermanentRevealed(playerPixel, pixelRadius);
        ApplyTextures();
    }

    void ScaleForIsometric()
    {
        float scaleX = isometricMapSize.x / 10f;
        float scaleY = isometricMapSize.y / 10f;

        if (blackFogRenderer != null)
        {
            blackFogRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        if (visionRenderer != null)
        {
            float visionScale = visionRadius * 2f / 10f;
            visionRenderer.transform.localScale = new Vector3(visionScale, visionScale, 1f);
        }
    }

    Vector2Int IsometricWorldToPixel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        float x = (localPos.x + isometricMapSize.x * 0.5f) / isometricMapSize.x;
        float y = (localPos.y + isometricMapSize.y * 0.5f) / isometricMapSize.y;

        x = Mathf.Clamp01(x);
        y = Mathf.Clamp01(y);

        return new Vector2Int(
            Mathf.FloorToInt(x * (textureSize - 1)),
            Mathf.FloorToInt(y * (textureSize - 1))
        );
    }

    void ClearVisionTexture()
    {
        Color32 transparent = new Color32(0, 0, 0, 0);
        for (int i = 0; i < visionPixels.Length; i++)
        {
            visionPixels[i] = transparent;
        }
    }

    void DrawVisionCircle(Vector2Int center, int radius)
    {
        int radiusSqr = radius * radius;
        Color32 transparent = new Color32(0, 0, 0, 0);

        int startX = Mathf.Max(0, center.x - radius);
        int endX = Mathf.Min(textureSize - 1, center.x + radius);
        int startY = Mathf.Max(0, center.y - radius);
        int endY = Mathf.Min(textureSize - 1, center.y + radius);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                int dx = x - center.x;
                int dy = y - center.y;

                if (dx * dx + dy * dy <= radiusSqr)
                {
                    int index = y * textureSize + x;
                    visionPixels[index] = transparent;
                }
            }
        }
    }

    void UpdatePermanentRevealed(Vector2Int center, int radius)
    {
        int radiusSqr = radius * radius;
        Color32 visited = new Color32(0, 0, 0, 150);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSqr)
                {
                    int pixelX = center.x + x;
                    int pixelY = center.y + y;

                    if (pixelX >= 0 && pixelX < textureSize && pixelY >= 0 && pixelY < textureSize)
                    {
                        int index = pixelY * textureSize + pixelX;
                        if (!revealedPixels[index])
                        {
                            revealedPixels[index] = true;
                            blackFogPixels[index] = visited;
                        }
                    }
                }
            }
        }
    }

    void ApplyTextures()
    {
        blackFogTexture.SetPixels32(blackFogPixels);
        visionTexture.SetPixels32(visionPixels);

        blackFogTexture.Apply();
        visionTexture.Apply();
    }

    void CreateSprites()
    {
        if (blackFogRenderer != null)
        {
            Rect rect = new Rect(0, 0, textureSize, textureSize);
            blackFogRenderer.sprite = Sprite.Create(blackFogTexture, rect, new Vector2(0.5f, 0.5f), 100f);
            blackFogRenderer.sortingOrder = 10;
        }

        if (visionRenderer != null)
        {
            Rect rect = new Rect(0, 0, textureSize, textureSize);
            visionRenderer.sprite = Sprite.Create(visionTexture, rect, new Vector2(0.5f, 0.5f), 100f);
            visionRenderer.sortingOrder = 11;
        }
    }

    public void SetVisionRadius(float newRadius)
    {
        visionRadius = newRadius;
        ScaleForIsometric();
    }
}