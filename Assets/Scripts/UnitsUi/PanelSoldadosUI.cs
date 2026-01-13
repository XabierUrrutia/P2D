using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelSoldadosUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject panelRoot;
    public Button closeButton;
    public Button soldadoButton;
    public Button generalButton; // <--- Botón del General

    [Header("Textos de Coste")]
    public TextMeshProUGUI soldadoCostText;
    public TextMeshProUGUI generalCostText;

    [Header("DATOS DE UNIDADES (Arrastra los ScriptableObjects aquí)")]
    public BuildingData datosDelSoldado; // <--- Arrastra aquí el Data del Soldado Raso
    public BuildingData datosDelGeneral; // <--- Arrastra aquí el Data del General

    [Header("Configuración Económica")]
    public int costSoldado = 50;
    public int costGeneral = 200;

    [Header("Configuración de Población")]
    private int pobCosteSoldado = 1; // El soldado ocupa 1 cama
    private int pobCosteGeneral = 2; // El general ocupa 2 camas

    private EdificioClick currentBuilding = null;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);

        // Asignar funciones a los botones
        if (soldadoButton != null) soldadoButton.onClick.AddListener(OnRecruitSoldado);
        if (generalButton != null) generalButton.onClick.AddListener(OnRecruitGeneral);

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
        // Actualizar Textos
        if (soldadoCostText != null) soldadoCostText.text = $"Soldado: {costSoldado}$";
        if (generalCostText != null) generalCostText.text = $"General: {costGeneral}$";

        bool hasMoney = MoneyManager.Instance != null;
        bool hasPop = PopulationManager.Instance != null;
        int currentMoney = hasMoney ? MoneyManager.Instance.CurrentMoney : 0;

        // --- LÓGICA BOTÓN SOLDADO ---
        if (soldadoButton != null)
        {
            bool puedePagar = currentMoney >= costSoldado;
            bool tieneSitio = hasPop && PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteSoldado);
            soldadoButton.interactable = puedePagar && tieneSitio;
        }

        // --- LÓGICA BOTÓN GENERAL ---
        if (generalButton != null)
        {
            bool puedePagar = currentMoney >= costGeneral;
            // El general ocupa MÁS espacio (pobCosteGeneral = 2)
            bool tieneSitio = hasPop && PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteGeneral);
            generalButton.interactable = puedePagar && tieneSitio;
        }
    }

    public void ConfigurarPanel(EdificioClick edificio)
    {
        currentBuilding = edificio;
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateUI();
    }

    public EdificioClick GetCurrentBuilding() { return currentBuilding; }

    // =========================================================
    //               RECLUTAR SOLDADO
    // =========================================================
    public void OnRecruitSoldado()
    {
        // 1. Check Población
        if (PopulationManager.Instance != null && !PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteSoldado))
        {
            Debug.Log("¡Necesitas más camas para el Soldado!");
            return;
        }

        // 2. Check Dinero
        if (MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney < costSoldado)
        {
            Debug.Log("No tienes dinero para el Soldado.");
            return;
        }

        // 3. ¡A CONSTRUIR!
        if (datosDelSoldado != null)
        {
            // HidePanel(); // Descomenta si quieres que se cierre el menú
            BuildingManager.Instance.SelectBuilding(datosDelSoldado);
        }
        else
        {
            Debug.LogError("Falta asignar 'Datos Del Soldado' en el Inspector.");
        }
    }

    // =========================================================
    //               RECLUTAR GENERAL
    // =========================================================
    public void OnRecruitGeneral()
    {
        // 1. Check Población (Ojo: usa pobCosteGeneral que vale 2)
        if (PopulationManager.Instance != null && !PopulationManager.Instance.HayEspacio(PopulationManager.TipoUnidad.Soldado, pobCosteGeneral))
        {
            Debug.Log("¡Necesitas MÁS camas para el General (Ocupa 2)!");
            return;
        }

        // 2. Check Dinero
        if (MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney < costGeneral)
        {
            Debug.Log("No tienes dinero para el General.");
            return;
        }

        // 3. ¡A CONSTRUIR!
        if (datosDelGeneral != null)
        {
            // HidePanel(); // Descomenta si quieres que se cierre el menú
            BuildingManager.Instance.SelectBuilding(datosDelGeneral);
        }
        else
        {
            Debug.LogError("Falta asignar 'Datos Del General' en el Inspector.");
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentBuilding = null;
    }
}