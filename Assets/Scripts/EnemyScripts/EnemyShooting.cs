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
    public Transform player; // fallback

    private float nextFireTime;
    private EnemyAI enemyAI;
    private bool debugAtivo = true;

    // NOVO: referência à base (usamos a mesma que EnemyAI já tem)
    private Transform baseJogador;

    void Start()
    {
        enemyAI = GetComponent<EnemyAI>();

        if (enemyAI == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyAI component not found!");
        }
        else
        {
            baseJogador = enemyAI.baseJogador;
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
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            // Fallback: usar o jogador por defeito
            if (player != null)
                CheckAndShoot(player);
            return;
        }

        CheckAndShoot(target);
    }

    Transform GetCurrentTarget()
    {
        // Prioridade: base do jogador se estiver dentro do alcance de ataque
        if (baseJogador == null && enemyAI != null)
            baseJogador = enemyAI.baseJogador;

        if (baseJogador != null)
        {
            float distBase = Vector2.Distance(transform.position, baseJogador.position);
            if (distBase <= attackRange * 1.2f) // pequena folga
                return baseJogador;
        }

        // Depois: alvo actual do EnemyAI (soldado)
        if (enemyAI != null)
        {
            Transform aiTarget = enemyAI.GetJogadorAlvo();
            if (aiTarget != null)
                return aiTarget;
        }

        // Fallback: jogador atribuído manualmente
        return player;
    }

    void CheckAndShoot(Transform target)
    {
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);

        bool podeDisparar = dist <= attackRange &&
                            Time.time >= nextFireTime &&
                            (enemyAI == null || enemyAI.EstaPersiguiendoJogador() || target == baseJogador);

        if (podeDisparar)
        {
            ShootAtTarget(target);
            nextFireTime = Time.time + fireRate;

            if (debugAtivo)
                Debug.Log($"{gameObject.name}: Disparando en {target.name} a {dist:F1} unidades");
        }
    }

    void ShootAtTarget(Transform target)
    {
        if (bulletPrefab == null || weaponPoint == null || target == null) return;

        Vector2 dir = (target.position - weaponPoint.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);

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
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = dir * bulletSpeed;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Destroy(bullet, 3f);

        if (debugAtivo)
            Debug.DrawLine(weaponPoint.position, target.position, Color.red, 0.5f);
    }

    public bool PuedeDisparar()
    {
        Transform target = GetCurrentTarget();
        if (target == null) return false;

        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= attackRange && Time.time >= nextFireTime;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Transform target = GetCurrentTarget();
        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            Gizmos.color = dist <= attackRange ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}