using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    [Header("Configuración de Spawn alrededor de la Base")]
    public float spawnRadius = 5f;                   // Radio alrededor de la base
    public float minDistanceFromBase = 1f;           // Distancia mínima desde el centro de la base
    public bool spawnInCircle = true;                // Si es false, spawn en área cuadrada

    [Header("Opciones de Terreno")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public LayerMask obstacleLayer;                  // Capa para obstáculos que bloqueen el spawn

    [Header("Spawning por Grupos")]
    public bool spawnInWaves = false;
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 2f;

    private int enemiesSpawned = 0;

    void Start()
    {
        if (spawnInWaves)
        {
            StartCoroutine(SpawnWaves());
        }
        else
        {
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        int spawned = 0;
        int tries = 0;
        int maxTries = enemyCount * 10;

        while (spawned < enemyCount && tries < maxTries)
        {
            Vector3 spawnPosition = GetSpawnPositionAroundBase();

            if (IsValidSpawnPosition(spawnPosition))
            {
                Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                spawned++;
                enemiesSpawned++;
            }

            tries++;
        }

        Debug.Log($"Enemigos generados: {spawned} (tentativas: {tries})");
    }

    System.Collections.IEnumerator SpawnWaves()
    {
        while (enemiesSpawned < enemyCount)
        {
            int enemiesThisWave = Mathf.Min(enemiesPerWave, enemyCount - enemiesSpawned);
            int spawnedThisWave = 0;
            int tries = 0;

            while (spawnedThisWave < enemiesThisWave && tries < enemiesThisWave * 10)
            {
                Vector3 spawnPosition = GetSpawnPositionAroundBase();

                if (IsValidSpawnPosition(spawnPosition))
                {
                    Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    spawnedThisWave++;
                    enemiesSpawned++;
                }

                tries++;
            }

            Debug.Log($"Ola generada: {spawnedThisWave} enemigos");

            if (enemiesSpawned < enemyCount)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }

    Vector3 GetSpawnPositionAroundBase()
    {
        Vector3 basePosition = transform.position;

        if (spawnInCircle)
        {
            // Spawn en círculo alrededor de la base
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
            // Spawn en área cuadrada alrededor de la base
            float x = Random.Range(-spawnRadius, spawnRadius);
            float y = Random.Range(-spawnRadius, spawnRadius);

            // Asegurar distancia mínima
            if (Mathf.Abs(x) < minDistanceFromBase) x = Mathf.Sign(x) * minDistanceFromBase;
            if (Mathf.Abs(y) < minDistanceFromBase) y = Mathf.Sign(y) * minDistanceFromBase;

            return basePosition + new Vector3(x, y, 0f);
        }
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        // Verificar si está en el suelo
        if (!Physics2D.OverlapCircle(position, 0.3f, groundLayer))
        {
            return false;
        }

        // Verificar si está en agua
        if (Physics2D.OverlapCircle(position, 0.3f, waterLayer))
        {
            return false;
        }

        // Verificar si hay obstáculos
        if (obstacleLayer != 0 && Physics2D.OverlapCircle(position, 0.5f, obstacleLayer))
        {
            return false;
        }

        // Verificar que no esté demasiado cerca de otros enemigos (opcional)
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

    // Visualización en el Editor
    void OnDrawGizmosSelected()
    {
        // Dibujar área de spawn
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

        // Dibujar icono de spawner
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.5f, "EnemySpawner.png", true);
    }
}