using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 4;
    private int currentHealth;

    [Header("UI")]
    public Slider healthBar;
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0); // Posición encima del personaje

    [Header("Death Settings")]
    public float deathDelay = 1.5f; // Tiempo antes de cambiar escena tras morir
    public bool disableMovementOnDeath = true;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        // Configurar posición de la barra de vida
        if (healthBar != null)
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
    }

    void Update()
    {
        // Mantener la barra sobre el personaje
        if (healthBar != null && !isDead)
            healthBar.transform.position = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Si ya está muerto, no hacer nada

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = (float)currentHealth / maxHealth;
    }

    void Die()
    {
        if (isDead) return; // Evitar múltiples llamadas

        isDead = true;
        Debug.Log("¡Personaje muerto! Cargando escena Game Over...");

        // Aquí puedes agregar:
        // - Animación de muerte
        // - Sonido de muerte

        // Desactivar el movimiento del personaje
        if (disableMovementOnDeath)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true; // Hacerlo kinemático para que no responda a físicas
            }

            // Desactivar el collider
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;

            // Desactivar scripts de movimiento (si los tienes)
            // Ejemplo: 
            // GetComponent<PlayerMovement>().enabled = false;
        }

        // Ocultar la barra de vida
        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        // Cambiar a la escena Game Over después de un delay
        Invoke("LoadGameOverScene", deathDelay);
    }

    void LoadGameOverScene()
    {
        // Cargar la escena Game Over por nombre
        SceneManager.LoadScene("Game Over");
    }
}