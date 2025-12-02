using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 4;
    private int currentHealth;

    [Header("UI")]
    public Slider healthBar;
    public Vector3 healthBarOffset = new Vector3(0, 1f, 0);

    [Header("Health Bar Display")]
    public float showHealthBarTime = 3f;
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;

    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public bool disableMovementOnDeath = true;

    private bool isDead = false;
    private bool isRegistered = false;

    void Start()
    {
        currentHealth = maxHealth;

        // Registrar este jugador en el GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
            isRegistered = true;
        }

        // Ocultar la barra de vida al inicio
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            UpdateHealthBarPosition();
        }
    }

    void Update()
    {
        // Actualizar posición de la barra si está visible
        if (healthBar != null && healthBarVisible && !isDead)
        {
            UpdateHealthBarPosition();

            // Contar el tiempo y ocultar la barra si ha pasado el tiempo
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f && healthBarVisible)
            {
                HideHealthBar();
            }
        }
    }

    void UpdateHealthBarPosition()
    {
        if (Camera.main != null)
        {
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Mostrar la barra de vida cuando recibe daño
        ShowHealthBar();
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // MÉTODOS PARA MOSTRAR/OCULTAR LA BARRA DE VIDA
    void ShowHealthBar()
    {
        if (healthBar != null && !healthBarVisible)
        {
            healthBar.gameObject.SetActive(true);
            healthBarVisible = true;
        }

        // Reiniciar el temporizador
        healthBarTimer = showHealthBarTime;
    }

    void HideHealthBar()
    {
        if (healthBar != null && healthBarVisible)
        {
            healthBar.gameObject.SetActive(false);
            healthBarVisible = false;
        }
    }

    // MÉTODOS PARA CURAR
    public void Heal(int amount)
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        Debug.Log($"Curado: +{amount}. Vida actual: {currentHealth}/{maxHealth}");
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    void Die()
    {
        if (isDead || GameManager.Instance.IsGameOver()) return;

        isDead = true;
        Debug.Log("Personaje muerto!");

        // 🔊 SOM DE MORTE
        if (SoundColector.Instance != null)
        {
            // Se tiveres tag "Tank" → som de morte de tanque
            if (CompareTag("Tank"))
            {
                SoundColector.Instance.PlayTankDeath();
            }
            // (Opcional) se tiveres tag "Building", podes fazer:
            // else if (CompareTag("Building"))
            // {
            //     SoundColector.Instance.PlayBuildingDestroyed();
            // }
            else
            {
                // Qualquer outro -> infanteria
                SoundColector.Instance.PlayInfantryDeath();
            }
        }

        // Notificar al GameManager que este jugador ha muerto
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }

        // Notificar al EnemyManager que este jugador ha muerto
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(transform);
        }

        // Resto del código existente...
        HideHealthBar();

        if (disableMovementOnDeath)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
        }

        Invoke("DeactivatePlayer", deathDelay);
    }

    void DeactivatePlayer()
    {
        gameObject.SetActive(false);

        // Opcional: destruir el objeto completamente
        // Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Asegurarse de desregistrar si el objeto es destruido
        if (isRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }
    }

    // Método público para forzar mostrar/ocultar la barra si es necesario
    public void SetHealthBarVisible(bool visible)
    {
        if (visible)
        {
            ShowHealthBar();
        }
        else
        {
            HideHealthBar();
        }
    }

    // Método para revivir al jugador (si necesitas esta funcionalidad)
    public void Revive()
    {
        if (isDead)
        {
            isDead = false;
            currentHealth = maxHealth;
            gameObject.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayer(this);
                isRegistered = true;
            }

            // Reactivar componentes si es necesario
            if (disableMovementOnDeath)
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }

                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                    collider.enabled = true;
            }

            UpdateHealthBar();
        }
    }
}
