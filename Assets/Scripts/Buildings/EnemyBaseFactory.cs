using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum FactoryType
{
    Pequena,
    Mediana,
    Grande
}

public class EnemyBaseFactory : MonoBehaviour
{
    [Header("Configuración de Conquista")]
    public FactoryType factoryType;
    public float conquestTime = 10f;
    public float conquestRange = 5f;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 2f;
    public float decaySpeed = 0.5f;

    [Header("Generación de Dinero")]
    public int moneyPerInterval = 25;
    public float moneyInterval = 5f;

    [Header("UI Elements")]
    public Slider conquestSlider;
    public Vector3 sliderOffset = new Vector3(0, 0, 0);

    [Header("Configuración de Fog of War")]
    public float visionRadius = 5f;  // Radio de visión cuando está conquistada

    [Header("Spawn Configuration")]
    public bool enableSpawning = false;  // Por defecto falso - lo controla el manager
    public GameObject enemyPrefab;
    public float spawnRadius = 3f;
    public float minSpawnDistance = 1.5f;
    public float spawnIntervalNormal = 10f;
    public float spawnIntervalDefensive = 5f;
    public int maxConcurrentEnemiesNormal = 2;
    public int maxConcurrentEnemiesDefensive = 3;
    public int totalToSpawn = 0;

    [Header("Terrain Validation")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public LayerMask waterLayer;

    [Header("Estados")]
    public bool isConquered = false;
    public bool isGeneratingMoney = false;
    public bool isActiveSpawner = false;  // Nueva variable

    // Variables internas
    private float conquestProgress = 0f;
    private Canvas sliderCanvas;
    private List<GameObject> conqueringPlayers = new List<GameObject>();
    private SpriteRenderer spriteRenderer;

    // Spawn internals
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private Coroutine moneyGenerationCoroutine;
    private int spawnedCount = 0;

    // Fog of War
    private FogStaticVision fogStaticVision;
    private bool fogVisionInitialized = false;

    // Colores para estados
    private Color neutralColor = Color.gray;
    private Color conqueringColor = Color.yellow;
    private Color conqueredColor = Color.green;

    void Start()
    {
        InitializeFactory();
        SetValuesByType();

        // Establecer sprite inicial
        if (spriteRenderer != null)
        {
            spriteRenderer.color = neutralColor;
        }

        // Inicializar componente de visión de niebla (desactivado inicialmente)
        InitializeFogVision();

        // Registrar con el FactorySpawnManager
        if (FactorySpawnManager.Instance != null)
        {
            FactorySpawnManager.Instance.RegisterFactory(this);
        }
        else
        {
            Debug.LogWarning($"[{name}] FactorySpawnManager no encontrado. Spawn desactivado.");
        }

        // NO iniciar spawn automáticamente - lo controla el manager
        // TryStartSpawning() será llamado por el manager
    }

    void InitializeFactory()
    {
        // Configurar collider para conquista
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = conquestRange;

        // Rigidbody para triggers
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        // Sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void InitializeFogVision()
    {
        // Buscar o crear el componente FogStaticVision
        fogStaticVision = GetComponent<FogStaticVision>();
        if (fogStaticVision == null)
        {
            fogStaticVision = gameObject.AddComponent<FogStaticVision>();
        }

        // Configurar parámetros
        fogStaticVision.visionRadius = visionRadius;
        fogStaticVision.alwaysActive = true;

        // Desactivar inicialmente (solo se activará cuando sea conquistada)
        fogStaticVision.enabled = false;

        fogVisionInitialized = true;
        Debug.Log($"[{name}] Componente FogStaticVision inicializado (inicialmente desactivado)");
    }

    void SetValuesByType()
    {
        switch (factoryType)
        {
            case FactoryType.Pequena:
                conquestTime = 10f;
                moneyPerInterval = 10;
                moneyInterval = 5f;
                maxConcurrentEnemiesNormal = 2;
                maxConcurrentEnemiesDefensive = 3;
                spawnIntervalNormal = 12f;
                spawnIntervalDefensive = 6f;
                visionRadius = 5f;  // Radio menor para fábrica pequeña
                break;
            case FactoryType.Mediana:
                conquestTime = 15f;
                moneyPerInterval = 25;
                moneyInterval = 5f;
                maxConcurrentEnemiesNormal = 3;
                maxConcurrentEnemiesDefensive = 5;
                spawnIntervalNormal = 8f;
                spawnIntervalDefensive = 4f;
                visionRadius = 7.5f;  // Radio mediano para fábrica mediana
                break;
            case FactoryType.Grande:
                conquestTime = 20f;
                moneyPerInterval = 50;
                moneyInterval = 5f;
                maxConcurrentEnemiesNormal = 5;
                maxConcurrentEnemiesDefensive = 8;
                spawnIntervalNormal = 6f;
                spawnIntervalDefensive = 2f;
                visionRadius = 10f;  // Radio mayor para fábrica grande
                break;
        }

        // Actualizar el radio de visión en el componente FogStaticVision si ya existe
        if (fogStaticVision != null)
        {
            fogStaticVision.visionRadius = visionRadius;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        if (!isConquered)
        {
            UpdateConquestProgress();
        }

        // Actualizar posición del slider para seguir al edificio
        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            UpdateSliderPosition();
        }
    }

    void UpdateSliderPosition()
    {
        if (sliderCanvas != null)
        {
            sliderCanvas.transform.position = transform.position + sliderOffset;

            // Hacer que el slider mire a la cámara (billboard)
            if (Camera.main != null)
            {
                sliderCanvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    void UpdateConquestProgress()
    {
        if (conqueringPlayers.Count > 0)
        {
            // Incrementar progreso con múltiples jugadores
            float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
            conquestProgress += progressIncrement * Time.deltaTime;
            conquestProgress = Mathf.Min(conquestProgress, conquestTime);

            // Mostrar slider
            if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(true);
                UpdateSliderPosition();
            }

            // Actualizar color
            spriteRenderer.color = Color.Lerp(neutralColor, conqueringColor, conquestProgress / conquestTime);
        }
        else if (conquestProgress > 0)
        {
            // Decrementar progreso si no hay jugadores
            float progressDecrement = decaySpeed / conquestTime;
            conquestProgress -= progressDecrement * Time.deltaTime;
            conquestProgress = Mathf.Max(conquestProgress, 0);

            // Ocultar slider si llega a 0
            if (conquestProgress <= 0 && conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(false);
                spriteRenderer.color = neutralColor;
            }
            else
            {
                // Actualizar color durante decrecimiento
                spriteRenderer.color = Color.Lerp(neutralColor, conqueringColor, conquestProgress / conquestTime);
            }
        }

        // Actualizar slider
        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            conquestSlider.value = conquestProgress / conquestTime;
        }

        // Completar conquista
        if (conquestProgress >= conquestTime && !isConquered)
        {
            CompleteConquest();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || isConquered) return;

        if (other.CompareTag("Player") && !conqueringPlayers.Contains(other.gameObject))
        {
            conqueringPlayers.Add(other.gameObject);
            Debug.Log($"[{name}] Jugador entró. Total: {conqueringPlayers.Count}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;

        if (other.CompareTag("Player") && conqueringPlayers.Contains(other.gameObject))
        {
            conqueringPlayers.Remove(other.gameObject);
            Debug.Log($"[{name}] Jugador salió. Total: {conqueringPlayers.Count}");
        }
    }

    private void CompleteConquest()
    {
        isConquered = true;
        spriteRenderer.color = conqueredColor;

        if (conquestSlider != null)
        {
            conquestSlider.gameObject.SetActive(false);
        }

        // Limpiar lista de jugadores
        conqueringPlayers.Clear();

        Debug.Log($"[{name}] ¡FÁBRICA CONQUISTADA!");

        // Activar visión en la niebla
        ActivateFogVision();

        // Detener spawns
        StopSpawning();

        // Iniciar generación de dinero
        StartMoneyGeneration();

        // Dar recompensa inmediata por conquistar
        GiveConquestReward();

        // Notificar al manager que esta fábrica fue conquistada
        // (El manager detectará esto en su chequeo periódico)
    }

    private void ActivateFogVision()
    {
        if (!fogVisionInitialized)
        {
            InitializeFogVision();
        }

        if (fogStaticVision != null)
        {
            fogStaticVision.enabled = true;

            // Forzar reinicialización para asegurar que se registre en el sistema de niebla
            if (fogStaticVision.isInitialized)
            {
                // Si ya estaba inicializado, reactivar
                FogOfWar fog = FindObjectOfType<FogOfWar>();
                if (fog != null)
                {
                    fog.UnregisterStaticVision(fogStaticVision);
                    fog.RegisterStaticVision(fogStaticVision);
                    fog.RequestUpdate();
                }
            }
            else
            {
                // El componente se inicializará automáticamente en su Start()
                // Podemos forzar la inicialización si es necesario
                var method = fogStaticVision.GetType().GetMethod("InitializeFogSystem",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(fogStaticVision, null);
                }
            }

            Debug.Log($"[{name}] Visión de niebla ACTIVADA con radio: {visionRadius}");
        }
        else
        {
            Debug.LogWarning($"[{name}] No se encontró componente FogStaticVision");
        }
    }

    private void StartMoneyGeneration()
    {
        if (!isGeneratingMoney)
        {
            isGeneratingMoney = true;
            moneyGenerationCoroutine = StartCoroutine(GenerateMoney());
        }
    }

    private IEnumerator GenerateMoney()
    {
        while (isConquered)
        {
            yield return new WaitForSeconds(moneyInterval);
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(moneyPerInterval);
                Debug.Log($"[{name}] Generados {moneyPerInterval} de dinero");
            }
        }
    }

    private void GiveConquestReward()
    {
        int reward = 0;
        switch (factoryType)
        {
            case FactoryType.Pequena:
                reward = 100;
                break;
            case FactoryType.Mediana:
                reward = 200;
                break;
            case FactoryType.Grande:
                reward = 400;
                break;
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(reward);
            Debug.Log($"[{name}] ¡Recompensa de {reward} por conquistar!");
        }
    }

    // ----- Sistema de Spawn -----
    public void TryStartSpawning()
    {
        if (isConquered || !enableSpawning || enemyPrefab == null) return;

        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnLoop());
            isActiveSpawner = true;
            Debug.Log($"[{name}] Spawn iniciado (ACTIVADO POR MANAGER)");
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (!isConquered && enableSpawning)
        {
            // Determinar modo actual
            bool isDefensiveMode = conqueringPlayers.Count > 0;
            float currentSpawnInterval = isDefensiveMode ? spawnIntervalDefensive : spawnIntervalNormal;
            int currentMaxEnemies = isDefensiveMode ? maxConcurrentEnemiesDefensive : maxConcurrentEnemiesNormal;

            CleanupNullSpawned();

            bool canSpawn = (totalToSpawn == 0 || spawnedCount < totalToSpawn) &&
                           spawnedEnemies.Count < currentMaxEnemies;

            if (canSpawn)
            {
                Vector3 spawnPosition = GetValidSpawnPosition();
                if (spawnPosition != Vector3.zero)
                {
                    SpawnEnemyAtPosition(spawnPosition);
                }
            }

            yield return new WaitForSeconds(currentSpawnInterval);
        }

        spawnCoroutine = null;
        isActiveSpawner = false;
    }

    private Vector3 GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 spawnPosition = GetRandomPositionAroundFactory();

            if (IsValidSpawnPosition(spawnPosition))
            {
                return spawnPosition;
            }
        }

        return Vector3.zero;
    }

    private Vector3 GetRandomPositionAroundFactory()
    {
        // Generar posición en círculo alrededor de la fábrica
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minSpawnDistance, spawnRadius);

        Vector2 offset = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance
        );

        return transform.position + new Vector3(offset.x, offset.y, 0f);
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Verificar terreno
        if (!Physics2D.OverlapCircle(position, 0.5f, groundLayer))
            return false;

        // Verificar agua
        if (Physics2D.OverlapCircle(position, 0.5f, waterLayer))
            return false;

        // Verificar obstáculos
        if (obstacleLayer != 0 && Physics2D.OverlapCircle(position, 0.5f, obstacleLayer))
            return false;

        // Verificar que no esté muy cerca de otros enemigos
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, 1f);
        foreach (Collider2D collider in nearbyEnemies)
        {
            if (collider.CompareTag("Enemy"))
                return false;
        }

        // Verificar que no esté dentro del edificio
        float distanceToCenter = Vector3.Distance(position, transform.position);
        if (distanceToCenter < 1f)
            return false;

        return true;
    }

    private void SpawnEnemyAtPosition(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.name = $"{enemyPrefab.name}_from_{name}_{spawnedCount}";
        spawnedEnemies.Add(enemy);
        spawnedCount++;

        // Obtener referencia al EnemyAI
        var enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            // Configurar para atacar la base automáticamente
            if (enemyAI.baseJogador == null)
            {
                GameObject playerBase = GameObject.FindGameObjectWithTag("PlayerBase");
                if (playerBase != null)
                {
                    enemyAI.baseJogador = playerBase.transform;
                }
            }

            // Activar patrullaje (irán a la base y patrullarán alrededor)
            enemyAI.SetUsarPatrullaje(true);

            // Registrar en sistemas
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.RegistrarEnemy(enemyAI);
            }
        }

        Debug.Log($"[{name}] Enemigo spawnado en {position} (total: {spawnedCount})");
    }

    private void CleanupNullSpawned()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
                spawnedEnemies.RemoveAt(i);
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        enableSpawning = false;
        isActiveSpawner = false;
        Debug.Log($"[{name}] Spawning detenido");
    }

    public bool IsConquered()
    {
        return isConquered;
    }
    // ---------------------------

    void OnDrawGizmosSelected()
    {
        // Rango de conquista
        Gizmos.color = isConquered ? Color.green : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red);
        Gizmos.DrawWireSphere(transform.position, conquestRange);

        // Rango de spawn
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, minSpawnDistance);

        // Posición del slider
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + sliderOffset, 0.2f);

        // Radio de visión de niebla (solo si está conquistada o en diseño)
        if (isConquered || Application.isEditor && !Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRadius);
        }

        // Indicador visual si es el spawner activo
        if (isActiveSpawner)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }

    void OnDestroy()
    {
        if (sliderCanvas != null)
        {
            Destroy(sliderCanvas.gameObject);
        }

        // Desregistrar del sistema de niebla si está activo
        if (fogStaticVision != null && fogStaticVision.isInitialized)
        {
            FogOfWar fog = FindObjectOfType<FogOfWar>();
            if (fog != null)
            {
                fog.UnregisterStaticVision(fogStaticVision);
            }
        }
    }

    // Método para reiniciar la fábrica (si implementas recaptura)
    public void ResetFactory()
    {
        isConquered = false;
        conquestProgress = 0f;
        spriteRenderer.color = neutralColor;

        // Desactivar visión de niebla
        if (fogStaticVision != null && fogStaticVision.enabled)
        {
            fogStaticVision.enabled = false;
            FogOfWar fog = FindObjectOfType<FogOfWar>();
            if (fog != null)
            {
                fog.UnregisterStaticVision(fogStaticVision);
                fog.RequestUpdate();
            }
        }

        // Detener generación de dinero
        isGeneratingMoney = false;
        if (moneyGenerationCoroutine != null)
        {
            StopCoroutine(moneyGenerationCoroutine);
            moneyGenerationCoroutine = null;
        }

        // Limpiar lista de conquistadores
        conqueringPlayers.Clear();

        Debug.Log($"[{name}] Fábrica reiniciada (neutra)");
    }
}