using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Componente para adicionar vida (HP) a uma Base do jogador.
/// - Regista a base no EnemyManager para que inimigos possam considerá-la como alvo.
/// - Aceita dano por balas inimigas (Bullet.OnTriggerEnter2D já aplica dano a objetos com tag "Player").
/// - Ao morrer carrega imediatamente a cena "Game Over".
/// </summary>
[DisallowMultipleComponent]
public class PlayerBase : MonoBehaviour
{
    [Header("HP")]
    [Tooltip("Vida máxima da base")]
    public int maxHealth = 20;
    [Tooltip("Slider opcional para mostrar a vida")]
    public Slider healthBar;

    [Header("Tag")]
    [Tooltip("Tag a aplicar ao GameObject para que balas inimigas o reconheçam (deixe vazio para não alterar)")]
    public string runtimeTagToApply = "Player";

    [Header("Feedback")]
    [Tooltip("Tempo durante o qual o feedback de dano permanece (se aplicável)")]
    public float damageFeedbackSeconds = 0.2f;

    [Header("Cores do Slider")]
    [Tooltip("Cor quando em bom estado (acima de 50 HP)")]
    public Color healthyColor = Color.green;
    [Tooltip("Cor quando em aviso (<= 50 HP)")]
    public Color warningColor = Color.yellow;
    [Tooltip("Cor quando crítico (<= 30% da vida máxima)")]
    public Color criticalColor = Color.red;

    private int currentHealth;
    private bool isDestroyed = false;

    void Awake()
    {
        currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
    }

    void Start()
    {
        // Aplica tag se definida e existir
        if (!string.IsNullOrEmpty(runtimeTagToApply))
        {
            try
            {
                gameObject.tag = runtimeTagToApply;
            }
            catch
            {
                Debug.LogWarning($"[PlayerBase] Tag '{runtimeTagToApply}' não existe. Defina a tag manualmente no Inspector se necessário.");
            }
        }

        // Inicializa UI
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            healthBar.gameObject.SetActive(true);
            UpdateHealthBarColor();
        }

        // Registar como "jogador" para que EnemyManager e inimigos o conheçam
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegistrarNovoJogador(this.transform);
            Debug.Log($"[PlayerBase] Registrada no EnemyManager: {name}");
        }
    }

    /// <summary>
    /// Aplica dano à base. Chamado por balas inimigas ou outras fontes.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;
        if (amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            UpdateHealthBarColor();
        }

        // opcional: efeito visual / som (pode ligar aqui)
        StartCoroutine(DamageFeedbackCoroutine());

        Debug.Log($"[PlayerBase] {name} recebeu {amount} de dano. HP = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }

    System.Collections.IEnumerator DamageFeedbackCoroutine()
    {
        // Placeholder para feedback (piscar sprite, etc.)
        if (damageFeedbackSeconds > 0f)
            yield return new WaitForSeconds(damageFeedbackSeconds);
        else
            yield break;
    }

    void OnDestroyed()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[PlayerBase] {name} FOI DESTRUID. Carregando Game Over...");

        // Opcional: evitar que GameManager interprete a destruição como "transição"
        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();

        // Carregar cena de Game Over (nome deve corresponder ao build settings)
        SceneManager.LoadScene("Game Over");
    }

    void OnDestroy()
    {
        // Ao destruir, avisar EnemyManager para remover como alvo (fallback)
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RemoverJogador(this.transform);
        }
    }

    // API utilitária
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    // Permite curar a base via código, se necessário
    public void Heal(int amount)
    {
        if (isDestroyed || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
            UpdateHealthBarColor();
        }
    }

    // Atualiza a cor do fill do slider conforme thresholds:
    // - crítico: <= 30% da vida máxima -> vermelho
    // - aviso: <= 50 HP -> amarelo
    // - saudável: caso contrário -> verde (ou healthyColor)
    void UpdateHealthBarColor()
    {
        if (healthBar == null) return;

        Image fillImage = null;
        if (healthBar.fillRect != null)
            fillImage = healthBar.fillRect.GetComponent<Image>();

        if (fillImage == null)
        {
            // tenta encontrar Image em filho chamado "Fill"
            var img = healthBar.GetComponentInChildren<Image>();
            if (img != null) fillImage = img;
        }

        if (fillImage == null) return;

        // prioridade ao crítico (30% da vida máxima)
        float criticalThreshold = maxHealth * 0.3f;
        if (currentHealth <= criticalThreshold)
        {
            fillImage.color = criticalColor;
        }
        else if (currentHealth <= 250)
        {
            fillImage.color = warningColor;
        }
        else
        {
            fillImage.color = healthyColor;
        }
    }
}