using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public Rigidbody2D rb;

    // Nova flag para identificar origem da bala
    public bool isEnemyBullet = false;

    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction * speed;
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D hit)
    {
        if (hit.CompareTag("Enemy") && !isEnemyBullet)
        {
            // Tenta encontrar qualquer tipo de script de vida
            var tutorialEnemy = hit.GetComponent<TutorialEnemyHealth>();
            var classicEnemy = hit.GetComponent<EnemyHealth>();

            if (tutorialEnemy != null)
            {
                Debug.Log($"[Bullet] Atingiu inimigo '{hit.name}' (TutorialEnemyHealth) e causou {damage} de dano");
                tutorialEnemy.TakeDamage(damage);
            }
            else if (classicEnemy != null)
            {
                Debug.Log($"[Bullet] Atingiu inimigo '{hit.name}' (EnemyHealth) e causou {damage} de dano");
                classicEnemy.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[Bullet] '{hit.name}' não tem script de vida reconhecido!");
            }

            Destroy(gameObject);
            return;
        }



        // Bala inimiga atinge jogador
        if (hit.CompareTag("Player") && isEnemyBullet)
        {
            PlayerHealth player = hit.GetComponent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // Opcional: destruir quando bate em obstáculos (layer checks)...
    }
}