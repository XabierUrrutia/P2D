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
        // DEBUG: Ver información de la colisión
        Debug.Log($"[Bullet] Colisión con: {hit.name}, Tag: {hit.tag}, Layer: {LayerMask.LayerToName(hit.gameObject.layer)}");

        // 1. BALA DEL JUGADOR - Golpea enemigos
        if (hit.CompareTag("Enemy") && !isEnemyBullet)
        {
            // Tenta encontrar qualquer tipo de script de vida
            var tutorialEnemy = hit.GetComponent<TutorialEnemyHealth>();
            var classicEnemy = hit.GetComponent<EnemyHealth>();

            // También buscar en el padre si no está en el collider directo
            if (tutorialEnemy == null && hit.transform.parent != null)
                tutorialEnemy = hit.transform.parent.GetComponent<TutorialEnemyHealth>();
            if (classicEnemy == null && hit.transform.parent != null)
                classicEnemy = hit.transform.parent.GetComponent<EnemyHealth>();

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

        // 2. BALA ENEMIGA - Golpea jugadores, generales y base (todos con IHealth)
        if ((hit.CompareTag("Player") || hit.CompareTag("General") || hit.CompareTag("PlayerBase")) && isEnemyBullet)
        {
            // ESTRATEGIA 1: Buscar IHealth directamente
            IHealth health = hit.GetComponent<IHealth>();

            // ESTRATEGIA 2: Si no se encuentra, buscar en el padre
            if (health == null && hit.transform.parent != null)
            {
                health = hit.transform.parent.GetComponent<IHealth>();
            }

            // ESTRATEGIA 3: Buscar componentes específicos y convertirlos a IHealth
            if (health == null)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    health = playerHealth as IHealth;
                    Debug.Log($"[Bullet] Encontrado PlayerHealth en {hit.name}");
                }
            }

            if (health == null)
            {
                GeneralHealth generalHealth = hit.GetComponent<GeneralHealth>();
                if (generalHealth != null)
                {
                    health = generalHealth as IHealth;
                    Debug.Log($"[Bullet] Encontrado GeneralHealth en {hit.name}");
                }
            }

            if (health == null)
            {
                PlayerBase playerBase = hit.GetComponent<PlayerBase>();
                if (playerBase != null)
                {
                    health = playerBase as IHealth;
                    Debug.Log($"[Bullet] Encontrado PlayerBase (IHealth) en {hit.name}");
                }
            }

            // Aplicar daño si encontramos IHealth
            if (health != null)
            {
                if (!health.IsDead)
                {
                    health.TakeDamage(damage);
                    Debug.Log($"[Bullet] Atingiu {hit.name} (IHealth) causou {damage} de dano. Vida: {health.GetCurrentHealth()}/{health.GetMaxHealth()}");
                }
                else
                {
                    Debug.Log($"[Bullet] {hit.name} já está morto, ignorando");
                }

                Destroy(gameObject);
                return;
            }
            else
            {
                // ESTRATEGIA 4: Compatibilidad con PlayerBase antiguo (sin IHealth)
                PlayerBase baseComp = hit.GetComponent<PlayerBase>();
                if (baseComp != null)
                {
                    baseComp.TakeDamage(damage);
                    Debug.Log($"[Bullet] Atingiu PlayerBase (legado) {hit.name} causou {damage} de dano");
                    Destroy(gameObject);
                    return;
                }
            }

            // Si llegamos aquí y es un jugador/general/base pero no tiene IHealth, mostrar advertencia
            if (hit.CompareTag("Player") || hit.CompareTag("General") || hit.CompareTag("PlayerBase"))
            {
                Debug.LogWarning($"[Bullet] Objeto {hit.name} tiene tag Player/General/PlayerBase pero no tiene IHealth ni PlayerBase");
            }
        }

        // 3. DESTRUIR BALA CON OBSTÁCULOS
        if (hit.CompareTag("Obstacle") || hit.CompareTag("Wall") || hit.CompareTag("Terrain"))
        {
            Destroy(gameObject);
            return;
        }

        // 4. EVITAR COLISIONES CON EL PROPIO EQUIPO
        if ((hit.CompareTag("Enemy") && isEnemyBullet) ||
            ((hit.CompareTag("Player") || hit.CompareTag("General") || hit.CompareTag("PlayerBase")) && !isEnemyBullet))
        {
            // No hacer nada - evita dañar aliados
            return;
        }
    }
}