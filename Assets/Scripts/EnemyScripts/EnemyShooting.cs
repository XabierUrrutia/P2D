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
    public Transform player; // Mantener por compatibilidad, pero no usar directamente

    private float nextFireTime;
    private EnemyAI enemyAI;
    private bool debugAtivo = true;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();

        if (enemyAI == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyAI component not found!");
        }

        // Buscar jugador si no está asignado (para compatibilidad)
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        // Obtener el objetivo actual del EnemyAI
        Transform target = GetCurrentTarget();

        if (target == null)
        {
            // Si no hay objetivo del EnemyAI, usar el jugador por defecto (compatibilidad)
            if (player != null)
            {
                CheckAndShoot(player);
            }
            return;
        }

        // Disparar al objetivo actual del EnemyAI
        CheckAndShoot(target);
    }

    void CheckAndShoot(Transform target)
    {
        if (target == null) return;

        // Calcular distancia al objetivo
        float dist = Vector2.Distance(transform.position, target.position);

        // Verificar condiciones para disparar
        bool podeDisparar = dist <= attackRange &&
                           Time.time >= nextFireTime &&
                           enemyAI != null &&
                           enemyAI.EstaPersiguiendoJogador();

        if (podeDisparar)
        {
            ShootAtTarget(target);
            nextFireTime = Time.time + fireRate;

            if (debugAtivo)
                Debug.Log($"{gameObject.name}: Disparando en {target.name} a {dist:F1} unidades");
        }
    }

    Transform GetCurrentTarget()
    {
        // Usar siempre el objetivo del EnemyAI si está disponible
        if (enemyAI != null)
        {
            Transform aiTarget = enemyAI.GetJogadorAlvo();
            if (aiTarget != null)
            {
                return aiTarget;
            }
        }

        // Fallback: usar el jugador asignado manualmente
        return player;
    }

    void ShootAtTarget(Transform target)
    {
        if (bulletPrefab == null || weaponPoint == null || target == null) return;

        // Calcular dirección al objetivo
        Vector2 dir = (target.position - weaponPoint.position).normalized;

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

        // Rotar la bala en la dirección del movimiento
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Destruir bala después de un tiempo
        Destroy(bullet, 3f);

        // Debug visual
        if (debugAtivo)
        {
            Debug.DrawLine(weaponPoint.position, target.position, Color.red, 0.5f);
        }
    }

    // Método público para verificar si puede disparar
    public bool PuedeDisparar()
    {
        Transform target = GetCurrentTarget();
        if (target == null) return false;

        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= attackRange && Time.time >= nextFireTime;
    }

    // Para debug visual
    void OnDrawGizmosSelected()
    {
        // Dibujar rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Dibujar línea al objetivo actual
        Transform target = GetCurrentTarget();
        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            Gizmos.color = dist <= attackRange ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}