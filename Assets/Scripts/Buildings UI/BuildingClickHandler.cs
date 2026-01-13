using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class EdificioClick : MonoBehaviour
{
    // Seleccionas en el inspector qué es este edificio
    public enum TipoEdificio
    {
        CuartelSoldados,
        FabricaTanques
    }

    [Header("Configuración")]
    public TipoEdificio tipoDeEdificio = TipoEdificio.CuartelSoldados;

    [Header("Paneles UI")]
    public GameObject panelSoldadosUI;
    public GameObject panelTanquesUI;

    // Referencias internas
    private PanelSoldadosUI scriptPanelSoldados;
    private PanelTanquesUI scriptPanelTanques;
    private bool clickWasOnThisBuilding = false;

    void Start()
    {
        // Buscar referencias automáticamente si están vacías
        if (panelSoldadosUI == null) panelSoldadosUI = FindObjectOfType<PanelSoldadosUI>(true)?.gameObject;
        if (panelSoldadosUI != null) scriptPanelSoldados = panelSoldadosUI.GetComponent<PanelSoldadosUI>();

        if (panelTanquesUI == null) panelTanquesUI = FindObjectOfType<PanelTanquesUI>(true)?.gameObject;
        if (panelTanquesUI != null) scriptPanelTanques = panelTanquesUI.GetComponent<PanelTanquesUI>();
    }

    private void OnMouseDown()
    {
        // Evitar clic si tocamos la UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        clickWasOnThisBuilding = true;
        GameEvents.RaiseBuildingSelected();

        TogglePanel();
        SoundColector.Instance?.PlayUiPanelOpen();

        clickWasOnThisBuilding = false;
    }

    public void TogglePanel()
    {
        if (!clickWasOnThisBuilding) return;

        // Lógica según el tipo de edificio
        if (tipoDeEdificio == TipoEdificio.CuartelSoldados)
        {
            AbrirSoldados();
        }
        else if (tipoDeEdificio == TipoEdificio.FabricaTanques)
        {
            AbrirTanques();
        }
    }

    void AbrirSoldados()
    {
        if (scriptPanelSoldados == null) return;

        // Si ya está abierto este mismo, cerramos
        if (panelSoldadosUI.activeSelf && scriptPanelSoldados.GetCurrentBuilding() == this)
        {
            scriptPanelSoldados.HidePanel();
        }
        else
        {
            // Cerramos el otro por si acaso y abrimos este
            if (scriptPanelTanques != null) scriptPanelTanques.HidePanel();
            Time.timeScale = 0f;
            scriptPanelSoldados.ConfigurarPanel(this);
        }
    }

    void AbrirTanques()
    {
        if (scriptPanelTanques == null) return;

        if (panelTanquesUI.activeSelf && scriptPanelTanques.GetCurrentBuilding() == this)
        {
            scriptPanelTanques.HidePanel();
        }
        else
        {
            if (scriptPanelSoldados != null) scriptPanelSoldados.HidePanel();
            Time.timeScale = 0f;
            scriptPanelTanques.ConfigurarPanel(this); // Aquí es donde daba el error antes
        }
    }

    // --- FUNCIONES QUE LLAMAN LOS PANELES ---

    public void ReclutarSoldado(TipoSoldado tipo)
    {
        Debug.Log($"Reclutando Soldado: {tipo}");
    }

    public void ReclutarTanque()
    {
        Debug.Log($"Reclutando Tanque en {gameObject.name}");
    }

    public enum TipoSoldado { Infanteria, Arquero, Caballeria }
}