using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public Rigidbody2D rb;

    // Flag para identificar origen da bala
    public bool isEnemyBullet = false;

    // --- NUEVO: VETERANÍA ---
    // Referencia al soldado que disparó esta bala
    [HideInInspector] public UnitVeterancy ownerVeterancy;
    // Cuánta XP da por golpear (puedes ajustar esto)
    public int xpPorGolpe = 10;
    // ------------------------

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
        // DEBUG: Ver información de la colisión (Útil si sigue fallando algo)
        // Debug.Log($"[Bullet] Colisión con: {hit.name}, Tag: {hit.tag}");

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

            bool huboImpacto = false;

            if (tutorialEnemy != null)
            {
                // Debug.Log($"[Bullet] Atingiu inimigo '{hit.name}' (TutorialEnemyHealth)");
                tutorialEnemy.TakeDamage(damage);
                huboImpacto = true;
            }
            else if (classicEnemy != null)
            {
                // Debug.Log($"[Bullet] Atingiu inimigo '{hit.name}' (EnemyHealth)");
                classicEnemy.TakeDamage(damage);
                huboImpacto = true;
            }
            else
            {
                Debug.LogWarning($"[Bullet] '{hit.name}' no tiene script de vida reconocido!");
            }

            // --- NUEVO: DAR EXPERIENCIA AL TIRADOR ---
            if (huboImpacto && ownerVeterancy != null)
            {
                ownerVeterancy.GanarXP(xpPorGolpe);
            }
            // ----------------------------------------

            Destroy(gameObject);
            return;
        }

        // 2. BALA ENEMIGA - Golpea jugadores, generales y base
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
                if (playerHealth != null) health = playerHealth as IHealth;
            }

            if (health == null)
            {
                GeneralHealth generalHealth = hit.GetComponent<GeneralHealth>();
                if (generalHealth != null) health = generalHealth as IHealth;
            }

            if (health == null)
            {
                PlayerBase playerBase = hit.GetComponent<PlayerBase>();
                if (playerBase != null) health = playerBase as IHealth;
            }

            // Aplicar daño si encontramos IHealth
            if (health != null)
            {
                if (!health.IsDead)
                {
                    health.TakeDamage(damage);
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
                    Destroy(gameObject);
                    return;
                }
            }

            // Si llegamos aquí y es un jugador/general/base pero no tiene IHealth
            if (hit.CompareTag("Player") || hit.CompareTag("General") || hit.CompareTag("PlayerBase"))
            {
                Debug.LogWarning($"[Bullet] Objeto {hit.name} tiene tag Player/General pero no tiene script de vida.");
            }
        }

        // 3. DESTRUIR BALA CON OBSTÁCULOS (CORREGIDO EL ERROR DE TAGS)
        // NOTA: Asegúrate de crear los Tags "Obstacle", "Wall" y "Terrain" en Unity para que esto funcione.
        // Si Unity no encuentra el tag, dará error. Para evitar el error si te olvidas el tag, usamos try-catch o verificamos.
        // Pero lo más limpio es simplemente tener los Tags creados.
        try
        {
            if (hit.CompareTag("Obstacle") || hit.CompareTag("Wall") || hit.CompareTag("Terrain"))
            {
                Destroy(gameObject);
                return;
            }
        }
        catch (System.Exception)
        {
            // Si entra aquí es porque olvidaste crear el Tag en el editor
            // Debug.LogError("¡Falta un TAG en el Editor! Crea Obstacle, Wall y Terrain.");
            // Destruimos la bala por seguridad si choca con algo "Untagged" que sea capa obstáculo
            if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                Destroy(gameObject);
                return;
            }
        }

        // 4. EVITAR COLISIONES CON EL PROPIO EQUIPO
        if ((hit.CompareTag("Enemy") && isEnemyBullet) ||
            ((hit.CompareTag("Player") || hit.CompareTag("General") || hit.CompareTag("PlayerBase")) && !isEnemyBullet))
        {
            return;
        }
    }
}