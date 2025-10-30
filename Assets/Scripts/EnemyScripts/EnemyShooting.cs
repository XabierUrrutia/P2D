using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform weaponPoint;
    public float fireRate = 1.5f;
    public float bulletSpeed = 8f;
    public float attackRange = 6f;
    public int bulletDamage = 1;

    private float nextFireTime;
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange && Time.time >= nextFireTime)
        {
            ShootAtPlayer();
            nextFireTime = Time.time + fireRate;
        }
    }

    void ShootAtPlayer()
    {
        if (bulletPrefab == null || weaponPoint == null) return;

        Vector2 dir = (player.position - weaponPoint.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, weaponPoint.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(dir);
            b.isEnemyBullet = true;      // Marca imediatamente a bala como inimiga
            b.damage = bulletDamage;
            // opcional: b.speed = bulletSpeed;
        }

        // Não relies em setar tag por timing; usa a flag acima ou configura o prefab já com a tag/layer.
    }
}