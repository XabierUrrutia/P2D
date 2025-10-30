using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public Slider healthBar; // Slider (min=0, max=1)
    public Vector3 healthBarLocalOffset = new Vector3(0f, 1f, 0f);

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = 1f;
            UpdateHealthBar();

            // Se estiver num Canvas em World Space como filho do prefab
            healthBar.transform.localPosition = healthBarLocalOffset;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            UpdateHealthBar();

        Debug.Log($"Enemy '{name}' recebeu {amount} dano. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = (float)currentHealth / (float)maxHealth;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}