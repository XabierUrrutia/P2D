using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelTanquesUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject panelRoot;
    public Button closeButton;
    public Button tanqueButton;
    public TextMeshProUGUI costText;

    [Header("Datos del Tanque")]
    // ARRASTRA AQUÍ EL SCRIPTABLE OBJECT DEL TANQUE
    public BuildingData datosDelTanque;

    [Header("Costes")]
    public int costTanque = 200;
    private int pobCosteTanque = 1;

    private EdificioClick currentBuilding = null;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
        if (tanqueButton != null) tanqueButton.onClick.AddListener(OnRecruitTanque);

        UpdateUI();
    }

    void OnEnable()
    {
        if (PopulationManager.Instance != null)
            PopulationManager.Instance.OnPopulationChanged += UpdateUI;

        UpdateUI();
    }

    void OnDisable()
    {
        if (PopulationManager.Instance != null)
            PopulationManager.Instance.OnPopulationChanged -= UpdateUI;
    }

    void UpdateUI()
    {
        if (costText != null) costText.text = $"Coste: {costTanque}$";

        if (tanqueButton != null)
        {
            bool tieneDinero = true;
            bool tieneSitio = true;

            if (MoneyManager.Instance != null)
                tieneDinero = MoneyManager.Instance.CurrentMoney >= costTanque;

            if (PopulationManager.Instance != null)
                tieneSitio = PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Tanque, pobCosteTanque);

            tanqueButton.interactable = tieneDinero && tieneSitio;
        }
    }

    public void ConfigurarPanel(EdificioClick edificio)
    {
        currentBuilding = edificio;
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateUI();
    }

    public EdificioClick GetCurrentBuilding() { return currentBuilding; }

    public void OnRecruitTanque()
    {
        // 1. CHEQUEO DE POBLACIÓN
        if (PopulationManager.Instance != null)
        {
            if (!PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Tanque, pobCosteTanque))
            {
                Debug.Log("¡Necesitas construir más Garajes!");
                return;
            }
        }

        // 2. CHEQUEO DE DINERO
        if (MoneyManager.Instance != null)
        {
            if (MoneyManager.Instance.CurrentMoney < costTanque)
            {
                Debug.Log("No tienes dinero suficiente.");
                return;
            }
        }

        // 3. ÉXITO
        if (datosDelTanque != null)
        {
            Debug.Log("Requisitos cumplidos. Activando fantasma del tanque...");

            // --- CAMBIO AQUÍ: HE QUITADO EL HidePanel() ---
            // HidePanel(); // Ahora está comentado, así que el panel SE QUEDA.

            // Pasamos la orden al constructor
            BuildingManager.Instance.SelectBuilding(datosDelTanque);
        }
        else
        {
            Debug.LogError("¡ERROR! Falta asignar el BuildingData en el Inspector.");
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentBuilding = null;
    }
}