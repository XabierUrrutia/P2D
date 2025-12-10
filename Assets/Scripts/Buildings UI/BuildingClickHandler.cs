using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handler de clique para edifícios.
/// REQUER um Collider2D no GameObject para OnMouseDown funcionar.
/// Abre o painel de soldados apenas para edifícios do tipo "Hangar".
/// Congela o jogo quando o painel está aberto.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EdificioClick : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Panel UI de soldados (arraste aqui o GameObject da cena, igual ao WinPanel)")]
    public GameObject panelSoldadosUI;

    [Header("Restrições")]
    [Tooltip("Apenas edifícios com este nome podem abrir o painel (deixe vazio para permitir todos)")]
    public string requiredBuildingName = "Hangar";

    [Tooltip("Ou use tag em vez de nome (deixe vazio para não usar tag)")]
    public string requiredBuildingTag = "";

    private PanelSoldadosUI panelScript;
    private Collider2D buildingCollider;
    private bool clickWasOnThisBuilding = false; // Flag de segurança

    void Awake()
    {
        // Garantir que tem Collider2D
        buildingCollider = GetComponent<Collider2D>();
        if (buildingCollider == null)
        {
            Debug.LogError($"[EdificioClick] '{gameObject.name}' NÃO TEM Collider2D! Adicione um BoxCollider2D ou PolygonCollider2D para o clique funcionar.");
            enabled = false;
            return;
        }

        // IMPORTANTE: OnMouseDown funciona melhor quando Is Trigger está DESMARCADO
        if (buildingCollider.isTrigger)
        {
            Debug.LogWarning($"[EdificioClick] '{gameObject.name}' tem Collider2D como Trigger. DESMARQUE 'Is Trigger' para OnMouseDown funcionar corretamente!");
        }
    }

    void Start()
    {
        BuscarPanel();

        // Validar se este edifício pode usar o painel
        if (!CanOpenPanel())
        {
            enabled = false;
            Debug.Log($"[EdificioClick] Edifício '{gameObject.name}' não é do tipo permitido ('{requiredBuildingName}'). Componente desabilitado.");
        }
    }

    bool CanOpenPanel()
    {
        // Se nome está definido, verifica nome
        if (!string.IsNullOrEmpty(requiredBuildingName))
        {
            if (gameObject.name.Contains(requiredBuildingName))
                return true;
        }

        // Se tag está definida, verifica tag
        if (!string.IsNullOrEmpty(requiredBuildingTag))
        {
            try
            {
                if (gameObject.CompareTag(requiredBuildingTag))
                    return true;
            }
            catch
            {
                // Tag não existe, ignora
            }
        }

        // Se nenhum critério foi definido, permite todos
        if (string.IsNullOrEmpty(requiredBuildingName) && string.IsNullOrEmpty(requiredBuildingTag))
            return true;

        return false;
    }

    void BuscarPanel()
    {
        if (panelSoldadosUI != null)
        {
            panelScript = panelSoldadosUI.GetComponent<PanelSoldadosUI>();
            if (panelScript == null)
            {
                Debug.LogError($"[EdificioClick] O GameObject '{panelSoldadosUI.name}' não tem o componente PanelSoldadosUI.");
            }
            else
            {
                Debug.Log($"[EdificioClick] PanelSoldadosUI configurado: {panelSoldadosUI.name}");
            }
            return;
        }

        // Fallback: procurar automaticamente
        panelScript = FindObjectOfType<PanelSoldadosUI>(true);
        if (panelScript != null)
        {
            panelSoldadosUI = panelScript.gameObject;
            Debug.Log($"[EdificioClick] PanelSoldadosUI encontrado automaticamente: {panelSoldadosUI.name}");
            return;
        }

        Debug.LogError("[EdificioClick] PanelSoldadosUI não encontrado. Arraste o painel no Inspector.");
    }

    /// <summary>
    /// OnMouseDown é chamado APENAS quando clica no Collider2D deste GameObject.
    /// É a ÚNICA forma legítima de abrir o painel.
    /// </summary>
    private void OnMouseDown()
    {
        // Verifica se não está clicando sobre UI (ex: botões)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[EdificioClick] Clique ignorado: está sobre UI.");
            return;
        }

        clickWasOnThisBuilding = true; // Marca que o clique foi legítimo
        Debug.Log($"[EdificioClick] ✓ Clique LEGÍTIMO detectado em '{gameObject.name}'!");
        TogglePanel();
        clickWasOnThisBuilding = false; // Reseta flag
    }

    /// <summary>
    /// IMPORTANTE: Este método APENAS funciona se foi chamado via OnMouseDown (flag de segurança).
    /// Previne que cliques fora do edifício abram o painel.
    /// </summary>
    public void TogglePanel()
    {
        // BLOQUEIO: Se não foi clicado neste edifício, ignora
        if (!clickWasOnThisBuilding)
        {
            Debug.LogWarning($"[EdificioClick] TogglePanel() chamado SEM clique no edifício '{gameObject.name}'. BLOQUEADO por segurança.");
            return;
        }

        if (panelScript == null)
        {
            BuscarPanel();
        }

        if (panelScript == null)
        {
            Debug.LogError("[EdificioClick] PanelSoldadosUI não encontrado.");
            return;
        }

        // Se o painel está aberto e é este edifício que o abriu, fecha
        if (panelSoldadosUI.activeSelf && panelScript.GetCurrentBuilding() == this)
        {
            panelScript.HidePanel();
            Time.timeScale = 1f;
            Debug.Log($"[EdificioClick] Painel fechado. Jogo descongelado.");
        }
        else
        {
            // Abre/reconfigura para este edifício
            Time.timeScale = 0f;
            panelScript.ConfigurarPanel(this);
            Debug.Log($"[EdificioClick] Painel aberto para '{gameObject.name}'. Jogo congelado.");
        }
    }

    /// <summary>
    /// DEPRECADO: Use apenas OnMouseDown. Este método está aqui para compatibilidade mas é bloqueado.
    /// </summary>
    public void MostrarPanel()
    {
        Debug.LogWarning($"[EdificioClick] MostrarPanel() foi chamado mas está BLOQUEADO. Use apenas clique direto no edifício.");
        // NÃO chama TogglePanel() porque não tem a flag de segurança
    }

    public void ReclutarSoldado(TipoSoldado tipoSoldado)
    {
        Debug.Log($"[EdificioClick] Reclutando {tipoSoldado} no edifício {gameObject.name}.");
        // Implementa aqui spawn/gestão de unidades
    }

    public enum TipoSoldado
    {
        Infanteria,
        Arquero,
        Caballeria
    }

    // Gizmo para debug: mostra o collider no Scene view
    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}