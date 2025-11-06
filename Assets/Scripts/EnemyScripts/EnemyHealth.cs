using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public int maxHealth = 5;
    public Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Slider de Vida")]
    public Slider healthSlider; // Asigna este Slider desde el Inspector

    [Header("Health Bar Display")]
    public float showHealthBarTime = 3f; // Tiempo que se muestra la barra después de daño
    public bool alwaysShowHealthBar = false; // Opción para mostrar siempre la barra

    private int currentHealth;
    private Camera mainCamera;
    private float healthBarTimer = 0f;
    private bool healthBarVisible = false;
    private Canvas healthBarCanvas;

    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        // Configurar el Slider
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;

            // Posicionar el slider encima del enemigo
            healthSlider.transform.SetParent(transform);
            healthSlider.transform.localPosition = healthBarOffset;

            // Configurar para mundo isométrico
            SetupWorldSpaceSlider();

            // Ocultar la barra al inicio si no está configurada para mostrarse siempre
            if (!alwaysShowHealthBar)
            {
                healthSlider.gameObject.SetActive(false);
                healthBarVisible = false;
            }
            else
            {
                healthBarVisible = true;
            }
        }
        else
        {
            Debug.LogError($"No hay Slider asignado para la barra de vida del enemigo: {name}");
        }
    }

    void SetupWorldSpaceSlider()
    {
        // Asegurarse de que el slider esté en World Space
        Canvas canvas = healthSlider.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Crear un Canvas si no existe
            GameObject canvasGO = new GameObject("HealthBarCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = healthBarOffset;

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Mover el slider al nuevo canvas
            healthSlider.transform.SetParent(canvasGO.transform);
            healthSlider.transform.localPosition = Vector3.zero;

            healthBarCanvas = canvas;
        }
        else
        {
            healthBarCanvas = canvas;
        }

        // Configurar el tamaño del slider
        RectTransform sliderRT = healthSlider.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(150, 20);
        sliderRT.localScale = Vector3.one * 0.01f; // Escala para mundo isométrico
    }

    void Update()
    {
        // Hacer que la barra siempre mire hacia la cámara
        if (healthSlider != null && mainCamera != null)
        {
            healthSlider.transform.rotation = mainCamera.transform.rotation;
        }

        // Actualizar temporizador de la barra de vida si está visible
        if (healthSlider != null && healthBarVisible && !alwaysShowHealthBar)
        {
            healthBarTimer -= Time.deltaTime;
            if (healthBarTimer <= 0f)
            {
                HideHealthBar();
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Mostrar la barra de vida cuando recibe daño
        if (!alwaysShowHealthBar)
        {
            ShowHealthBar();
        }

        UpdateHealthBar();

        Debug.Log($"Enemy '{name}' recibió {amount} de daño. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    // MÉTODOS PARA MOSTRAR/OCULTAR LA BARRA DE VIDA
    void ShowHealthBar()
    {
        if (healthSlider != null && !healthBarVisible)
        {
            healthSlider.gameObject.SetActive(true);
            healthBarVisible = true;
        }

        // Reiniciar el temporizador
        healthBarTimer = showHealthBarTime;
    }

    void HideHealthBar()
    {
        if (healthSlider != null && healthBarVisible)
        {
            healthSlider.gameObject.SetActive(false);
            healthBarVisible = false;
        }
    }

    void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthSlider.value = healthPercent;
        }
    }

    void Die()
    {
        // Destruir la barra de vida
        if (healthSlider != null)
            Destroy(healthSlider.gameObject);

        // Si tenemos un canvas separado, destruirlo también
        if (healthBarCanvas != null)
            Destroy(healthBarCanvas.gameObject);

        Destroy(gameObject);
    }

    // MÉTODOS PÚBLICOS ADICIONALES (opcionales)
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

    // Método para forzar mostrar/ocultar la barra
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

    // Método para curar al enemigo (útil si hay mecánicas de curación)
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        // Opcional: mostrar barra brevemente al curar
        if (!alwaysShowHealthBar)
        {
            ShowHealthBar();
        }
    }
}