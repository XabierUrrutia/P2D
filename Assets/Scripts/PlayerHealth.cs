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

    [Header("Death Settings")]
    public float deathDelay = 1.5f;
    public bool disableMovementOnDeath = true;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        if (healthBar != null)
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
    }

    void Update()
    {
        if (healthBar != null && !isDead)
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // MÉTODOS NUEVOS PARA CURAR
    public void Heal(int amount)
    {
        if (isDead) return;

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
            // Asegurarnos de que el valor del slider esté entre 0 y 1
            healthBar.value = (float)currentHealth / maxHealth;
            Debug.Log($"Actualizando barra: {currentHealth}/{maxHealth} = {healthBar.value}");
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Personaje muerto! Cargando escena Game Over...");

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

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        Invoke("LoadGameOverScene", deathDelay);
    }

    void LoadGameOverScene()
    {
        SceneManager.LoadScene("Game Over");
    }
}