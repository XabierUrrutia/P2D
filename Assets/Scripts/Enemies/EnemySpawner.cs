using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 10;
    public Vector2 spawnAreaMin = new Vector2(-20, -20);
    public Vector2 spawnAreaMax = new Vector2(20, 20);
    public LayerMask groundLayer;
    public LayerMask waterLayer;

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int spawned = 0;
        int tries = 0;

        while (spawned < enemyCount && tries < enemyCount * 10)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                0
            );

            if (IsOnGround(pos) && !IsInWater(pos))
            {
                Instantiate(enemyPrefab, pos, Quaternion.identity);
                spawned++;
            }

            tries++;
        }

        Debug.Log($"Inimigos gerados: {spawned}");
    }

    bool IsOnGround(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, groundLayer);
    }

    bool IsInWater(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, waterLayer);
    }
}
