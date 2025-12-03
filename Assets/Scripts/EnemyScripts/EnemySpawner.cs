using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    [Header("Configuración de Spawn alrededor de la Base")]
    public float spawnRadius = 5f;
    public float minDistanceFromBase = 1f;
    public bool spawnInCircle = true;

    [Header("Opciones de Terreno")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public LayerMask obstacleLayer;

    [Header("Spawning por Grupos")]
    public bool spawnInWaves = false;
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 2f;

    private int enemiesSpawned = 0;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    private int currentWaveNumber = 0;

    void Start()
    {
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.AddSpawner(this);
        }
    }

    public void StartSpawning()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("No hay prefab de enemigo asignado en el spawner: " + name);
            return;
        }

        spawningActive = true;
        enemiesSpawned = 0;
        currentWaveNumber++;

        if (spawnInWaves)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            spawnCoroutine = StartCoroutine(SpawnWaves());
        }
        else
        {
            StartCoroutine(SpawnAllAtOnce());
        }
    }

    IEnumerator SpawnAllAtOnce()
    {
        int spawned = 0;
        int tries = 0;
        int maxTries = enemyCount * 10;

        while (spawned < enemyCount && tries < maxTries && spawningActive)
        {
            Vector3 spawnPosition = GetSpawnPositionAroundBase();

            if (IsValidSpawnPosition(spawnPosition))
            {
                GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                RegisterEnemyWithSystems(enemy);
                spawned++;
                enemiesSpawned++;
            }

            tries++;

            // Pequeño delay para no sobrecargar el frame
            if (spawned % 5 == 0)
            {
                yield return null;
            }
        }

        Debug.Log($"Spawner {name}: Generados {spawned} enemigos (oleada {currentWaveNumber})");

        // Notificar que terminó
        SpawningFinished();
    }

    public IEnumerator SpawnWaves()
    {
        int waveNumber = 1;

        while (enemiesSpawned < enemyCount && spawningActive)
        {
            Debug.Log($"Spawner {name}: Iniciando ola {waveNumber} (enemigos restantes: {enemyCount - enemiesSpawned})");

            int enemiesThisWave = Mathf.Min(enemiesPerWave, enemyCount - enemiesSpawned);
            int spawnedThisWave = 0;
            int tries = 0;
            int maxTries = enemiesThisWave * 10;

            while (spawnedThisWave < enemiesThisWave && tries < maxTries && spawningActive)
            {
                Vector3 spawnPosition = GetSpawnPositionAroundBase();

                if (IsValidSpawnPosition(spawnPosition))
                {
                    GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    RegisterEnemyWithSystems(enemy);
                    spawnedThisWave++;
                    enemiesSpawned++;
                }

                tries++;
            }

            Debug.Log($"Spawner {name}: Ola {waveNumber} completada - {spawnedThisWave} enemigos. Total: {enemiesSpawned}/{enemyCount}");

            waveNumber++;

            // Esperar entre oleadas solo si aún quedan enemigos por spawnear
            if (enemiesSpawned < enemyCount && spawningActive)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        Debug.Log($"Spawner {name}: Todas las olas completadas. Total generados: {enemiesSpawned}");

        // Notificar que terminó
        SpawningFinished();
    }

    void SpawningFinished()
    {
        spawningActive = false;
        spawnCoroutine = null;

        // Notificar al WaveManager que este spawner terminó
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.NotifySpawnerFinished();
        }
    }

    public void StopSpawning()
    {
        spawningActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        Debug.Log($"Spawner {name}: Spawning detenido. Generados {enemiesSpawned}/{enemyCount} enemigos");
    }

    void RegisterEnemyWithSystems(GameObject enemy)
    {
        if (EnemyManager.Instance != null)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                EnemyManager.Instance.RegistrarEnemy(enemyAI);
            }
        }

        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.RegisterEnemy(enemy);
        }

        if (EnemyWaveManager.Instance != null && EnemyWaveManager.Instance.IsRevengeWaveActive())
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetUsarPatrullaje(false);
            }
        }
    }

    Vector3 GetSpawnPositionAroundBase()
    {
        Vector3 basePosition = transform.position;

        if (spawnInCircle)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(minDistanceFromBase, spawnRadius);

            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );

            return basePosition + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            float x = Random.Range(-spawnRadius, spawnRadius);
            float y = Random.Range(-spawnRadius, spawnRadius);

            if (Mathf.Abs(x) < minDistanceFromBase) x = Mathf.Sign(x) * minDistanceFromBase;
            if (Mathf.Abs(y) < minDistanceFromBase) y = Mathf.Sign(y) * minDistanceFromBase;

            return basePosition + new Vector3(x, y, 0f);
        }
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        if (!Physics2D.OverlapCircle(position, 0.3f, groundLayer))
        {
            return false;
        }

        if (Physics2D.OverlapCircle(position, 0.3f, waterLayer))
        {
            return false;
        }

        if (obstacleLayer != 0 && Physics2D.OverlapCircle(position, 0.5f, obstacleLayer))
        {
            return false;
        }

        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(position, 1f);
        foreach (Collider2D collider in nearbyEnemies)
        {
            if (collider.CompareTag("Enemy"))
            {
                return false;
            }
        }

        return true;
    }

    public void SetWaveParameters(int count, int perWave, float betweenWaves)
    {
        enemyCount = count;
        enemiesPerWave = perWave;
        timeBetweenWaves = betweenWaves;
    }

    public void ResetSpawner()
    {
        enemiesSpawned = 0;
        spawningActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public bool IsSpawningActive()
    {
        return spawningActive;
    }

    public int GetEnemiesSpawned()
    {
        return enemiesSpawned;
    }

    public int GetTotalEnemiesToSpawn()
    {
        return enemyCount;
    }

    public float GetSpawnProgress()
    {
        if (enemyCount == 0) return 1f;
        return (float)enemiesSpawned / enemyCount;
    }

    void OnDestroy()
    {
        if (EnemyWaveManager.Instance != null)
        {
            EnemyWaveManager.Instance.RemoveSpawner(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (spawnInCircle)
        {
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            if (minDistanceFromBase > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, minDistanceFromBase);
            }
        }
        else
        {
            Vector3 size = new Vector3(spawnRadius * 2, spawnRadius * 2, 0.1f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}