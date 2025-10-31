using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    [Header("Spawn area (opções)")]
    public bool usePlayerAsCenter = true;            // se true usa o jogador como centro do raio
    public Transform playerTransform;                // opcional: arrastar o Transform do jogador no Inspector
    public string playerTag = "Player";              // se playerTransform estiver vazio, tenta encontrar por tag
    public float spawnRadius = 20f;                  // raio ao redor do centro (quando usePlayerAsCenter == true)

    [Tooltip("Somente usado quando usePlayerAsCenter == false")]
    public Vector2 spawnAreaMin = new Vector2(-20, -20);
    [Tooltip("Somente usado quando usePlayerAsCenter == false")]
    public Vector2 spawnAreaMax = new Vector2(20, 20);

    public LayerMask groundLayer;
    public LayerMask waterLayer;

    void Start()
    {
        // tenta obter o player se necessário
        if (usePlayerAsCenter && playerTransform == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) playerTransform = go.transform;
        }

        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int spawned = 0;
        int tries = 0;

        while (spawned < enemyCount && tries < enemyCount * 20)
        {
            Vector3 pos;

            if (usePlayerAsCenter)
            {
                Vector3 center = (playerTransform != null) ? playerTransform.position : transform.position;
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                pos = new Vector3(center.x + offset.x, center.y + offset.y, 0f);
            }
            else
            {
                pos = new Vector3(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                    0f
                );
            }

            if (IsOnGround(pos) && !IsInWater(pos))
            {
                Instantiate(enemyPrefab, pos, Quaternion.identity);
                spawned++;
            }

            tries++;
        }

        Debug.Log($"Inimigos gerados: {spawned} (tentativas: {tries})");
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