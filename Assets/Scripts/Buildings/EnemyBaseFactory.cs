using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyBaseFactory : MonoBehaviour
{
    [Header("Configuración de Conquista")]
    public float conquestTime = 30f;
    public float conquestRange = 5f;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 1f;
    public float decaySpeed = 0.5f;

    [Header("UI Elements")]
    public Slider conquestSlider;
    public Vector3 sliderPosition = new Vector3(-1309.61f, -144.46f, 0f);
    // victoryCanvas removido nesta versão sem win panel

    [Header("Estados")]
    public bool isConquered = false;

    // --- Spawn (opcional) ---
    [Header("Spawn (opcional)")]
    [Tooltip("Prefab do inimigo a spawnar")]
    public GameObject enemyPrefab;
    [Tooltip("Pontos onde os inimigos aparecem; se vazio usa a posição desta base")]
    public Transform[] spawnPoints;
    [Tooltip("Intervalo entre spawns")]
    public float spawnInterval = 5f;
    [Tooltip("Máximo de inimigos ativos gerados por esta base")]
    public int maxConcurrentEnemies = 3;
    [Tooltip("Total a spawnar (0 = infinito)")]
    public int totalToSpawn = 0;
    // --------------------------

    // Variables internas
    private float conquestProgress = 0f;
    private Canvas sliderCanvas;
    private List<PlayerBuildingDetector> conqueringPlayers = new List<PlayerBuildingDetector>();

    // Spawn internals
    private readonly List<GameObject> spawned = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int spawnedCount = 0;

    // Contador simples de jogadores dentro do trigger (fallback ao sistema de detector)
    private int playersInRange = 0;

    void Start()
    {
        InitializeBase();
        SetupSlider();

        // victoryCanvas removido: não há UI de vitória nesta versão
    }

    void InitializeBase()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = conquestRange;

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }
    }

    void SetupSlider()
    {
        if (conquestSlider != null)
        {
            GameObject canvasGO = new GameObject("EnemyBaseCanvas");
            canvasGO.transform.SetParent(transform);
            sliderCanvas = canvasGO.AddComponent<Canvas>();
            sliderCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = sliderCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);

            sliderCanvas.transform.position = sliderPosition;
            sliderCanvas.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            conquestSlider.transform.SetParent(sliderCanvas.transform);
            conquestSlider.minValue = 0f;
            conquestSlider.maxValue = 1f;
            conquestSlider.value = 0f;
            conquestSlider.gameObject.SetActive(false);

            conquestSlider.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        // Si el juego está pausado, no actualizar nada
        if (Time.timeScale == 0) return;

        if (!isConquered)
        {
            UpdateConquestProgress();
        }
    }

    void UpdateConquestProgress()
    {
        if (conqueringPlayers.Count > 0)
        {
            float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
            conquestProgress += progressIncrement * Time.deltaTime;
            conquestProgress = Mathf.Min(conquestProgress, conquestTime);

            if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(true);
            }
        }
        else if (conquestProgress > 0)
        {
            float progressDecrement = decaySpeed / conquestTime;
            conquestProgress -= progressDecrement * Time.deltaTime;
            conquestProgress = Mathf.Max(conquestProgress, 0);

            if (conquestProgress <= 0 && conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(false);
            }
        }

        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            conquestSlider.value = conquestProgress / conquestTime;
        }

        if (conquestProgress >= conquestTime && !isConquered)
        {
            CompleteConquest();
        }
    }

    public void RegisterPlayer(PlayerBuildingDetector player)
    {
        if (!conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Add(player);
            Debug.Log($"[EnemyBaseFactory] Player registered for conquest on {name}. Count = {conqueringPlayers.Count}");

            // iniciar spawn quando o primeiro jogador começa a tentar conquistar
            if (conqueringPlayers.Count == 1)
                TryStartSpawning();
        }
    }

    public void UnregisterPlayer(PlayerBuildingDetector player)
    {
        if (conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Remove(player);
            Debug.Log($"[EnemyBaseFactory] Player unregistered for conquest on {name}. Count = {conqueringPlayers.Count}");

            // parar spawns quando nenhum jogador estiver mais no alcance
            if (conqueringPlayers.Count == 0)
                StopSpawning();
        }
    }

    private void CompleteConquest()
    {
        isConquered = true;

        if (conquestSlider != null)
        {
            conquestSlider.gameObject.SetActive(false);
        }

        conqueringPlayers.Clear();

        Debug.Log("¡BASE ENEMIGA CONQUISTADA!");
        // Removida chamada e lógica do victoryCanvas / pausa do jogo

        // Parar spawns definitivamente se a base foi conquistada
        StopSpawning();
    }

    // --- Trigger detection como fallback/alternativa ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // prefere detectar por componente PlayerBuildingDetector (mais robusto), senão pela tag "Player"
        if (other.GetComponent<PlayerBuildingDetector>() != null || other.CompareTag("Player"))
        {
            playersInRange++;
            Debug.Log($"[EnemyBaseFactory] Player entrou no range de {name}. playersInRange={playersInRange}");
            TryStartSpawning();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;

        if (other.GetComponent<PlayerBuildingDetector>() != null || other.CompareTag("Player"))
        {
            playersInRange = Mathf.Max(0, playersInRange - 1);
            Debug.Log($"[EnemyBaseFactory] Player saiu do range de {name}. playersInRange={playersInRange}");
            if (playersInRange == 0 && conqueringPlayers.Count == 0)
                StopSpawning();
        }
    }

    // ----- Spawn logic -----
    private void TryStartSpawning()
    {
        // se já conquistada não spawna
        if (isConquered) return;
        if (enemyPrefab == null)
        {
            // nada a spawnar: evitar logs repetidos
            return;
        }

        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnLoop());
            Debug.Log($"[EnemyBaseFactory] SpawnLoop iniciado em {name}");
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (!isConquered)
        {
            CleanupNullSpawned();

            bool canSpawn = (totalToSpawn == 0 || spawnedCount < totalToSpawn) &&
                            spawned.Count < maxConcurrentEnemies &&
                            (playersInRange > 0 || conqueringPlayers.Count > 0); // spawn só com jogadores por perto

            if (canSpawn)
            {
                SpawnOne();
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
        }

        spawnCoroutine = null;
    }

    private void SpawnOne()
    {
        if (enemyPrefab == null) return;

        Transform pt = transform;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform chosen = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (chosen != null) pt = chosen;
        }

        GameObject go = Instantiate(enemyPrefab, pt.position, pt.rotation);
        go.name = enemyPrefab.name + "_spawn";
        spawned.Add(go);
        spawnedCount++;

        // Registrar no EnemyManager se o prefab tiver EnemyAI
        var enemyAi = go.GetComponent<EnemyAI>();
        if (enemyAi != null)
            EnemyManager.Instance?.RegistrarEnemy(enemyAi);

        Debug.Log($"[EnemyBaseFactory] Spawned {go.name} at {pt.position} (active spawned: {spawned.Count})");
    }

    private void CleanupNullSpawned()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null)
                spawned.RemoveAt(i);
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            Debug.Log($"[EnemyBaseFactory] SpawnLoop parado em {name}");
        }
    }
    // ---------------------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isConquered ? Color.blue : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red);
        Gizmos.DrawWireSphere(transform.position, conquestRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sliderPosition, 0.5f);
    }

    void OnDestroy()
    {
        if (sliderCanvas != null)
        {
            Destroy(sliderCanvas.gameObject);
        }
    }
}