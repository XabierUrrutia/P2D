using UnityEngine;
using System.Collections.Generic;

public class FogOfWar : MonoBehaviour
{
    [Header("Configuración del Mapa")]
    public string playerTag = "Player";
    public float defaultVisionRadius = 5f;
    public Vector2 isometricMapSize = new Vector2(300f, 200f);

    [Header("Posición del Mapa en el Mundo")]
    public Vector2 mapWorldPosition = new Vector2(-1369f, -146f);

    [Header("Renderers")]
    public SpriteRenderer blackFogRenderer;
    public SpriteRenderer visionRenderer;

    [Header("Texturas")]
    public int textureSize = 1024;

    private List<FogPlayer> fogPlayers = new List<FogPlayer>();
    private Texture2D blackFogTexture;
    private Texture2D visionTexture;
    private Color32[] blackFogPixels;
    private Color32[] visionPixels;
    private bool[] revealedPixels;
    private bool[] currentVisionPixels; // NUEVO: Para rastrear píxeles visibles actualmente

    private bool needsUpdate = false;
    private Bounds worldBounds;

    void Start()
    {
        CalculateWorldBounds();
        InitializeTextures();
        PositionFogRenderers();
        FindAllPlayers();

        Debug.Log($"FogOfWar inicializado. Mapa mundial: {mapWorldPosition}, Tamaño: {isometricMapSize}");
        Debug.Log($"Bounds del mundo: {worldBounds}");
    }

    void LateUpdate()
    {
        if (needsUpdate)
        {
            UpdateFogOfWar();
            needsUpdate = false;
        }
    }

    void CalculateWorldBounds()
    {
        // Calcular los bounds del mundo basados en la posición del mapa y su tamaño
        Vector3 center = new Vector3(
            mapWorldPosition.x + isometricMapSize.x * 0.5f,
            mapWorldPosition.y + isometricMapSize.y * 0.5f,
            0f
        );

        Vector3 size = new Vector3(isometricMapSize.x, isometricMapSize.y, 0f);
        worldBounds = new Bounds(center, size);
    }

    void FindAllPlayers()
    {
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject playerObj in playerObjs)
        {
            FogPlayer fogPlayer = playerObj.GetComponent<FogPlayer>();
            if (fogPlayer != null && !fogPlayers.Contains(fogPlayer))
            {
                fogPlayers.Add(fogPlayer);
                fogPlayer.SetFogOfWar(this);
            }
        }
        Debug.Log($"Encontrados {fogPlayers.Count} jugadores");

        if (fogPlayers.Count > 0)
        {
            needsUpdate = true;
        }
    }

    // Método público para que los jugadores se registren
    public void RegisterPlayer(FogPlayer fogPlayer)
    {
        if (!fogPlayers.Contains(fogPlayer))
        {
            fogPlayers.Add(fogPlayer);
            needsUpdate = true;
            Debug.Log($"Jugador registrado: {fogPlayer.name} en posición: {fogPlayer.transform.position}");
        }
    }

    // Método público para que los jugadores se eliminen
    public void UnregisterPlayer(FogPlayer fogPlayer)
    {
        if (fogPlayers.Contains(fogPlayer))
        {
            fogPlayers.Remove(fogPlayer);
            needsUpdate = true;
            Debug.Log($"Jugador eliminado: {fogPlayer.name}. Total: {fogPlayers.Count}");
        }
    }

    // Método para actualizar cuando un jugador se mueve
    public void RequestUpdate()
    {
        needsUpdate = true;
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
        currentVisionPixels = new bool[textureSize * textureSize]; // INICIALIZAR EL NUEVO ARRAY

        // Inicializar
        Color32 black = new Color32(0, 0, 0, 255);
        Color32 transparent = new Color32(0, 0, 0, 0);

        for (int i = 0; i < blackFogPixels.Length; i++)
        {
            blackFogPixels[i] = black;
            visionPixels[i] = transparent;
            revealedPixels[i] = false;
            currentVisionPixels[i] = false; // INICIALIZAR COMO NO VISIBLE
        }

        ApplyTextures();
        CreateSprites();
    }

    void UpdateFogOfWar()
    {
        if (fogPlayers.Count == 0) return;

        ClearVisionTexture();

        // Procesar cada jugador
        foreach (FogPlayer fogPlayer in fogPlayers)
        {
            if (fogPlayer != null && fogPlayer.gameObject.activeInHierarchy)
            {
                Vector3 playerWorldPos = fogPlayer.transform.position;
                Vector2Int playerPixel = WorldToPixel(playerWorldPos);

                // Calcular radio en píxeles
                float worldToPixelRatio = textureSize / Mathf.Max(isometricMapSize.x, isometricMapSize.y);
                int pixelRadius = Mathf.RoundToInt(fogPlayer.visionRadius * worldToPixelRatio);
                pixelRadius = Mathf.Max(1, pixelRadius);

                // Debug detallado
                if (Time.frameCount % 60 == 0) // Log cada segundo aprox
                {
                    Debug.Log($"Jugador: {fogPlayer.name} | " +
                             $"World: {playerWorldPos} | " +
                             $"Pixel: {playerPixel} | " +
                             $"Bounds: {worldBounds} | " +
                             $"Radio píxeles: {pixelRadius}");
                }

                DrawVisionCircle(playerPixel, pixelRadius);
                UpdatePermanentRevealed(playerPixel, pixelRadius);
            }
        }

        ApplyTextures();
    }

    Vector2Int WorldToPixel(Vector3 worldPos)
    {
        // Convertir posición mundial absoluta a coordenadas de textura
        // Considerando que el mapa empieza en mapWorldPosition
        float u = (worldPos.x - mapWorldPosition.x) / isometricMapSize.x;
        float v = (worldPos.y - mapWorldPosition.y) / isometricMapSize.y;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        int pixelX = Mathf.FloorToInt(u * (textureSize - 1));
        int pixelY = Mathf.FloorToInt(v * (textureSize - 1));

        return new Vector2Int(pixelX, pixelY);
    }

    void ClearVisionTexture()
    {
        Color32 transparent = new Color32(0, 0, 0, 0);
        for (int i = 0; i < visionPixels.Length; i++)
        {
            visionPixels[i] = transparent;
            currentVisionPixels[i] = false; // LIMPIAR VISIÓN ACTUAL
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
                int distSqr = dx * dx + dy * dy;

                if (distSqr <= radiusSqr)
                {
                    int index = y * textureSize + x;
                    visionPixels[index] = transparent;
                    currentVisionPixels[index] = true; // MARCAR COMO VISIBLE
                }
            }
        }
    }

    void UpdatePermanentRevealed(Vector2Int center, int radius)
    {
        int radiusSqr = radius * radius;
        Color32 visited = new Color32(0, 0, 0, 150);

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
                    if (!revealedPixels[index])
                    {
                        revealedPixels[index] = true;
                        blackFogPixels[index] = visited;
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

    void PositionFogRenderers()
    {
        // Posicionar los renderers en la posición correcta del mundo
        Vector3 rendererPosition = new Vector3(
            mapWorldPosition.x + isometricMapSize.x * 0.5f,
            mapWorldPosition.y + isometricMapSize.y * 0.5f,
            0f
        );

        if (blackFogRenderer != null)
        {
            blackFogRenderer.transform.position = rendererPosition;
            blackFogRenderer.transform.localScale = new Vector3(
                isometricMapSize.x / 10f,
                isometricMapSize.y / 10f,
                1f
            );
        }

        if (visionRenderer != null)
        {
            visionRenderer.transform.position = rendererPosition;
            visionRenderer.transform.localScale = new Vector3(
                isometricMapSize.x / 10f,
                isometricMapSize.y / 10f,
                1f
            );
        }
    }

    // NUEVO MÉTODO: Verificar si una posición está visible
    public bool IsPositionVisible(Vector3 worldPosition)
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
        int index = pixel.y * textureSize + pixel.x;

        if (index >= 0 && index < currentVisionPixels.Length)
        {
            return currentVisionPixels[index];
        }

        return false;
    }

    // NUEVO MÉTODO: Verificar si una posición ha sido revelada (niebla gris)
    public bool IsPositionRevealed(Vector3 worldPosition)
    {
        Vector2Int pixel = WorldToPixel(worldPosition);
        int index = pixel.y * textureSize + pixel.x;

        if (index >= 0 && index < revealedPixels.Length)
        {
            return revealedPixels[index];
        }

        return false;
    }

    // Debug visual en el editor
    void OnDrawGizmosSelected()
    {
        // Dibujar bounds del mundo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

        // Dibujar posición de cada jugador
        Gizmos.color = Color.green;
        foreach (FogPlayer player in fogPlayers)
        {
            if (player != null)
            {
                Gizmos.DrawSphere(player.transform.position, 0.5f);
                Gizmos.DrawWireSphere(player.transform.position, player.visionRadius);
            }
        }
    }

    void OnDestroy()
    {
        if (blackFogTexture != null) Destroy(blackFogTexture);
        if (visionTexture != null) Destroy(visionTexture);
        fogPlayers.Clear();
    }
}