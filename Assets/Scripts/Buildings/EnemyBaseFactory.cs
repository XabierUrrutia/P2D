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
    public Vector3 sliderOffset = new Vector3(0, 2f, 0);

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
                break;
            case FactoryType.Mediana:
                conquestTime = 15f;
                moneyPerInterval = 25;
                moneyInterval = 5f;
                maxConcurrentEnemiesNormal = 3;
                maxConcurrentEnemiesDefensive = 5;
                spawnIntervalNormal = 8f;
                spawnIntervalDefensive = 4f;
                break;
            case FactoryType.Grande:
                conquestTime = 20f;
                moneyPerInterval = 50;
                moneyInterval = 5f;
                maxConcurrentEnemiesNormal = 5;
                maxConcurrentEnemiesDefensive = 8;
                spawnIntervalNormal = 6f;
                spawnIntervalDefensive = 2f;
                break;
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

        // Detener spawns
        StopSpawning();

        // Iniciar generación de dinero
        StartMoneyGeneration();

        // Dar recompensa inmediata por conquistar
        GiveConquestReward();

        // Notificar al manager que esta fábrica fue conquistada
        // (El manager detectará esto en su chequeo periódico)
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
    }
}