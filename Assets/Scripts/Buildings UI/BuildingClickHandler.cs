using UnityEngine;

public class EdificioClick : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string panelTag = "PanelSoldados";
    [SerializeField] private TipoEdificio tipoEdificio = TipoEdificio.Cuartel;

    private GameObject panelSoldados;
    private PanelSoldadosUI panelScript;

    void Start()
    {
        // Buscar el panel por etiqueta
        BuscarPanel();
    }

    private void BuscarPanel()
    {
        GameObject panelObj = GameObject.FindGameObjectWithTag(panelTag);

        if (panelObj != null)
        {
            panelSoldados = panelObj;
            panelScript = panelSoldados.GetComponent<PanelSoldadosUI>();

            if (panelScript == null)
            {
                Debug.LogError("El panel no tiene el script PanelSoldadosUI");
            }
        }
        else
        {
            Debug.LogError($"No se encontró panel con la etiqueta: {panelTag}");
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        MostrarPanel();
    }

    public void MostrarPanel()
    {
        if (panelSoldados == null)
        {
            BuscarPanel();
            if (panelSoldados == null) return;
        }

        // Activar el panel y pasar la información de este edificio
        panelSoldados.SetActive(true);

        if (panelScript != null)
        {
            panelScript.ConfigurarPanel(this, tipoEdificio);
        }
    }

    // Método para reclutar soldados (llamado desde el panel)
    public void ReclutarSoldado(TipoSoldado tipoSoldado)
    {
        Debug.Log($"Reclutando {tipoSoldado} en {tipoEdificio} - {gameObject.name}");

        // Aquí implementarías la lógica real de reclutamiento
        switch (tipoSoldado)
        {
            case TipoSoldado.Infanteria:
                // Lógica para infantería
                break;
            case TipoSoldado.Arquero:
                // Lógica para arqueros
                break;
            case TipoSoldado.Caballeria:
                // Lógica para caballería
                break;
        }
    }

    // Enumeraciones para tipos
    public enum TipoEdificio
    {
        Cuartel,
        Establo,
        CuartelArqueros,
        Ayuntamiento
    }

    public enum TipoSoldado
    {
        Infanteria,
        Arquero,
        Caballeria
    }
}