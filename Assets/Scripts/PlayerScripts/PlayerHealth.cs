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
    public float showHealthBarTime = 3f; // Tiempo que se muestra la barra después de daño
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;

    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public bool disableMovementOnDeath = true;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        // Ocultar la barra de vida al inicio
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
        }
    }

    void Update()
    {
        // Actualizar posición de la barra si está visible
        if (healthBar != null && healthBarVisible && !isDead)
        {
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);

            // Contar el tiempo y ocultar la barra si ha pasado el tiempo
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f && healthBarVisible)
            {
                HideHealthBar();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

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
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        Debug.Log($"Curado: +{amount}. Vida actual: {currentHealth}/{maxHealth}");

        // Opcional: mostrar barra brevemente al curar también
        // ShowHealthBar();
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
            Debug.Log($"Actualizando barra: {currentHealth}/{maxHealth} = {healthBar.value}");
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Personaje muerto! Cargando escena Game Over...");

        // Ocultar la barra de vida al morir
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

        Invoke("LoadGameOverScene", deathDelay);
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("Game Over");
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
}