using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelTanquesUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public Button tanqueButton;
    public TextMeshProUGUI costText;
    public int costTanque = 200;

    private EdificioClick currentBuilding = null; // Acepta EdificioClick

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
        if (tanqueButton != null) tanqueButton.onClick.AddListener(OnRecruitTanque);
        if (costText != null) costText.text = $"Custo: {costTanque}";
    }

    // Recibe 'EdificioClick', NO 'EdificioTanqueClick'
    public void ConfigurarPanel(EdificioClick edificio)
    {
        currentBuilding = edificio;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public EdificioClick GetCurrentBuilding() { return currentBuilding; }

    public void OnRecruitTanque()
    {
        if (currentBuilding != null && MoneyManager.Instance != null)
        {
            if (MoneyManager.Instance.SpendMoney(costTanque))
            {
                currentBuilding.ReclutarTanque();
            }
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentBuilding = null;
        Time.timeScale = 1f;
    }
}