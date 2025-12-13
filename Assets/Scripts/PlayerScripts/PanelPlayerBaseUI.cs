using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Painel de gestão da PLAYER BASE.
/// Muito parecido com PanelSoldadosUI, mas apenas com a opção de CURAR a base.
/// - Anexar este script ao prefab/painel da UI da PlayerBase.
/// - O handler PlayerBaseBuildingClickHandler1 deve chamar ConfigurarPanel(thisBase).
/// </summary>
public class PanelPlayerBaseUI : MonoBehaviour
{
    [Header("Referências UI")]
    [Tooltip("Root do painel (normalmente inativo).")]
    public GameObject panelRoot;

    [Tooltip("Botão fechar do painel")]
    public Button closeButton;

    [Header("Informação de HP")]
    [Tooltip("Texto que mostra HP atual/max da PlayerBase.")]
    public TextMeshProUGUI hpText;

    [Tooltip("Slider opcional para mostrar HP da base.")]
    public Slider hpSlider;

    [Header("Botão de Cura")]
    public Button healButton;
    public TextMeshProUGUI healCostText;

    [Header("Custo de Cura")]
    [Tooltip("Custo em dinheiro para cada cura.")]
    public int healCost = 20;

    [Tooltip("Quantidade de HP curado por utilização.")]
    public int healAmount = 5;

    // estado
    private PlayerBase currentBase = null;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (healButton != null)
            healButton.onClick.AddListener(OnHealClicked);

        UpdateCostText();
    }

    void OnEnable()
    {
        RefreshButtonsInteractable();
        UpdateHpUI();
    }

    void UpdateCostText()
    {
        if (healCostText != null)
            healCostText.text = $"Custo: {healCost}";
    }

    void RefreshButtonsInteractable()
    {
        if (healButton == null) return;

        if (MoneyManager.Instance != null && currentBase != null)
        {
            int money = MoneyManager.Instance.CurrentMoney;
            bool hasHpToHeal = currentBase.GetCurrentHealth() < currentBase.GetMaxHealth();
            healButton.interactable = hasHpToHeal && money >= healCost;
        }
        else
        {
            healButton.interactable = true;
        }
    }

    /// <summary>
    /// API simplificada: chamada pelo handler da PlayerBase.
    /// </summary>
    public void ConfigurarPanel(PlayerBase playerBase)
    {
        currentBase = playerBase;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        UpdateCostText();
        UpdateHpUI();
        RefreshButtonsInteractable();
    }

    public PlayerBase GetCurrentBase()
    {
        return currentBase;
    }

    // Handler do botão de cura
    public void OnHealClicked()
    {
        TryHealBase();
    }

    private void TryHealBase()
    {
        if (currentBase == null)
        {
            Debug.LogWarning("[PanelPlayerBaseUI] Nenhuma PlayerBase associada.");
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PanelPlayerBaseUI] MoneyManager não encontrado.");
            return;
        }

        if (currentBase.GetCurrentHealth() >= currentBase.GetMaxHealth())
        {
            Debug.Log("[PanelPlayerBaseUI] Base já está com HP máximo.");
            RefreshButtonsInteractable();
            return;
        }

        if (!MoneyManager.Instance.SpendMoney(healCost))
        {
            Debug.Log("[PanelPlayerBaseUI] Dinheiro insuficiente para curar a base.");
            RefreshButtonsInteractable();
            return;
        }

        currentBase.Heal(healAmount);
        Debug.Log($"[PanelPlayerBaseUI] Base curada em {healAmount} HP por {healCost} moedas.");

        UpdateHpUI();
        RefreshButtonsInteractable();
    }

    void UpdateHpUI()
    {
        if (currentBase == null) return;

        int hp = currentBase.GetCurrentHealth();
        int maxHp = currentBase.GetMaxHealth();

        if (hpText != null)
            hpText.text = $"{hp} / {maxHp}";

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = hp;
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentBase = null;
        Time.timeScale = 1f; // Descongela o jogo ao fechar
        Debug.Log("[PanelPlayerBaseUI] Painel fechado. Jogo descongelado.");
    }
}