using UnityEngine;

public class Medkit : MonoBehaviour
{
    [Header("Configuración de Curación")]
    [SerializeField] private int fixedHealAmount = 2; // Cura exactamente 2 puntos
    [SerializeField] private bool destroyOnUse = true;

    [Header("Efectos")]
    [SerializeField] private AudioClip healSound;
    [SerializeField] private GameObject healEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (playerHealth != null && !playerHealth.IsFullHealth())
            {
                ApplyHeal(playerHealth);
            }
        }
    }

    private void ApplyHeal(PlayerHealth playerHealth)
    {
        int maxHealth = playerHealth.GetMaxHealth();
        int currentHealth = playerHealth.GetCurrentHealth();

        Debug.Log($"Antes de curar: {currentHealth}/{maxHealth}");

        // Calcular la curación real (no pasarse del máximo)
        int healAmount = Mathf.Min(fixedHealAmount, maxHealth - currentHealth);

        Debug.Log($"Curando: {healAmount} puntos");

        // Aplicar la curación
        playerHealth.Heal(healAmount);

        // Efectos
        PlayHealEffects();

        // Destruir o desactivar el medkit
        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
        else
        {
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void PlayHealEffects()
    {
        if (healSound != null)
        {
            AudioSource.PlayClipAtPoint(healSound, transform.position);
        }

        if (healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }
    }
}