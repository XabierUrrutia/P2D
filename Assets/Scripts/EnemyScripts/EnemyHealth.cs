using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public int maxHealth = 5;
    public Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Slider de Vida")]
    public Slider healthSlider; // Asigna este Slider desde el Inspector

    private int currentHealth;
    private Camera mainCamera;

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
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        Debug.Log($"Enemy '{name}' recibió {amount} de daño. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthSlider.value = healthPercent;

            // Cambiar color según la vida (opcional)
            UpdateHealthBarColor(healthPercent);
        }
    }

    void UpdateHealthBarColor(float healthPercent)
    {
        // Buscar la imagen de fill para cambiarle el color
        Transform fillArea = healthSlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null)
            {
                Image fillImage = fill.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (healthPercent > 0.6f)
                        fillImage.color = Color.green;
                    else if (healthPercent > 0.3f)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.red;
                }
            }
        }
    }

    void Die()
    {
        // Destruir la barra de vida
        if (healthSlider != null)
            Destroy(healthSlider.gameObject);

        Destroy(gameObject);
    }
}