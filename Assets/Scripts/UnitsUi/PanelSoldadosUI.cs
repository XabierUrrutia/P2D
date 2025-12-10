using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Painel de recrutamento simplificado: apenas dois tipos => Soldado e General.
/// Anexar ao prefab do painel (PanelSoldadosUI). EdificioClick chama ConfigurarPanel(...).
/// </summary>
public class PanelSoldadosUI : MonoBehaviour
{
    [Header("Referências UI")]
    [Tooltip("Root do painel (normalmente inativo).")]
    public GameObject panelRoot;

    [Tooltip("Botão fechar do painel")]
    public Button closeButton;

    [Header("Botões de Recrutamento")]
    public Button soldadoButton;
    public TextMeshProUGUI soldadoCostText;

    public Button generalButton;
    public TextMeshProUGUI generalCostText;

    [Header("Custos")]
    public int costSoldado = 10;
    public int costGeneral = 50;

    // estado
    private EdificioClick currentBuilding = null;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (soldadoButton != null) soldadoButton.onClick.AddListener(OnRecruitSoldado);
        if (generalButton != null) generalButton.onClick.AddListener(OnRecruitGeneral);

        UpdateCostTexts();
    }

    void OnEnable()
    {
        // Quando o painel for activado, atualizar estado dos botões (fundos)
        RefreshButtonsInteractable();
    }

    void UpdateCostTexts()
    {
        if (soldadoCostText != null) soldadoCostText.text = $"Custo: {costSoldado}";
        if (generalCostText != null) generalCostText.text = $"Custo: {costGeneral}";
    }

    void RefreshButtonsInteractable()
    {
        if (MoneyManager.Instance != null)
        {
            int money = MoneyManager.Instance.CurrentMoney;
            if (soldadoButton != null) soldadoButton.interactable = money >= costSoldado;
            if (generalButton != null) generalButton.interactable = money >= costGeneral;
        }
        else
        {
            if (soldadoButton != null) soldadoButton.interactable = true;
            if (generalButton != null) generalButton.interactable = true;
        }
    }

    /// <summary>
    /// API simplificada: chamada por EdificioClick.
    /// </summary>
    public void ConfigurarPanel(EdificioClick edificio)
    {
        currentBuilding = edificio;

        if (panelRoot != null) panelRoot.SetActive(true);

        UpdateCostTexts();
        RefreshButtonsInteractable();
    }

    /// <summary>
    /// Retorna o edifício atualmente associado ao painel (para toggle).
    /// </summary>
    public EdificioClick GetCurrentBuilding()
    {
        return currentBuilding;
    }

    // Handlers dos botões
    public void OnRecruitSoldado()
    {
        TryRecruitSoldado();
    }

    public void OnRecruitGeneral()
    {
        TryRecruitGeneral();
    }

    private void TryRecruitSoldado()
    {
        if (currentBuilding == null)
        {
            Debug.LogWarning("[PanelSoldadosUI] Nenhum edifício associado.");
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PanelSoldadosUI] MoneyManager não encontrado.");
            return;
        }

        if (!MoneyManager.Instance.SpendMoney(costSoldado))
        {
            Debug.Log("[PanelSoldadosUI] Dinheiro insuficiente para recrutar Soldado.");
            return;
        }

        currentBuilding.ReclutarSoldado(EdificioClick.TipoSoldado.Infanteria);
        Debug.Log($"[PanelSoldadosUI] Recrutado Soldado em {currentBuilding.name} por {costSoldado}.");

        RefreshButtonsInteractable();
    }

    private void TryRecruitGeneral()
    {
        if (currentBuilding == null)
        {
            Debug.LogWarning("[PanelSoldadosUI] Nenhum edifício associado.");
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PanelSoldadosUI] MoneyManager não encontrado.");
            return;
        }

        if (!MoneyManager.Instance.SpendMoney(costGeneral))
        {
            Debug.Log("[PanelSoldadosUI] Dinheiro insuficiente para recrutar General.");
            return;
        }

        currentBuilding.ReclutarSoldado(EdificioClick.TipoSoldado.Caballeria);
        Debug.Log($"[PanelSoldadosUI] Recrutado General em {currentBuilding.name} por {costGeneral}.");

        RefreshButtonsInteractable();
    }

    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentBuilding = null;
        Time.timeScale = 1f; // Descongela o jogo ao fechar
        Debug.Log("[PanelSoldadosUI] Painel fechado. Jogo descongelado.");
    }
}