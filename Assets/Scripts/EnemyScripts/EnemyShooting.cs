using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 1.5f;
    public float bulletSpeed = 8f;
    public float attackRange = 6f;
    public int bulletDamage = 1;

    [Header("Referencias")]
    public Transform player; // Ahora puedes asignar manualmente

    private float nextFireTime;
    private EnemyAI enemyAI;
    private bool debugAtivo = true;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();

        // Buscar jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player == null && debugAtivo)
        {
            Debug.LogError($"{gameObject.name}: Não foi possível encontrar o jogador para disparar!");
        }
    }

    void Update()
    {
        if (player == null)
        {
            // Intentar encontrar el jugador si se perdió la referencia
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        // Calcular distancia al jugador
        float dist = Vector2.Distance(transform.position, player.position);

        // Verificar condiciones para disparar
        bool podeDisparar = dist <= attackRange &&
                           Time.time >= nextFireTime &&
                           enemyAI != null &&
                           enemyAI.EstaPersiguiendoJogador();

        if (podeDisparar)
        {
            ShootAtPlayer();
            nextFireTime = Time.time + fireRate;

            if (debugAtivo)
                Debug.Log($"{gameObject.name}: Disparando no jogador a {dist} unidades de distância");
        }
    }

    void ShootAtPlayer()
    {
        if (bulletPrefab == null || weaponPoint == null || player == null) return;

        // Calcular dirección al jugador
        Vector2 dir = (player.position - weaponPoint.position).normalized;

        // Crear bala
        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

        // Configurar bala
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(dir);
            b.isEnemyBullet = true;
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
        }
        else
        {
            // Fallback si no hay componente Bullet
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = dir * bulletSpeed;
        }

        // Destruir bala después de un tiempo
        Destroy(bullet, 3f);
    }

    // Método público para verificar si puede disparar
    public bool PuedeDisparar()
    {
        if (player == null) return false;

        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= attackRange && Time.time >= nextFireTime;
    }

    // Para debug visual
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            Gizmos.color = dist <= attackRange ? Color.red : Color.white;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}