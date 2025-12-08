using UnityEngine;

/// <summary>
/// Anexa a cada unidade jogável / selecionável.
/// - Se 'isPlayerUnit' for true cria/usará a seta de seleção (FloatingArrow).
/// - ShowSelection(true/false) mostra/esconde a seta sem tocar na lógica de seleção ou movement.
/// </summary>
[DisallowMultipleComponent]
public class SelectableUnit : MonoBehaviour
{
    [Tooltip("Se true, esta unidade pertence ao jogador e pode mostrar a seta de seleção.")]
    public bool isPlayerUnit = true;

    [Tooltip("Prefab da seta (ex.: FloatingArrow). Opcional: se vazio, procura um filho já presente (apenas para unidades do jogador).")]
    public GameObject arrowPrefab;

    [Tooltip("Offset local da seta (unidade acima da unidade)")]
    public Vector3 arrowLocalOffset = new Vector3(0f, 0f, 0f);

    private GameObject arrowInstance;

    void Awake()
    {
        // Só criamos/ligamos a seta para unidades do jogador
        if (!isPlayerUnit) return;

        // procura child existente com componente FloatingArrow
        var existing = GetComponentInChildren<FloatingArrow>(true);
        if (existing != null)
        {
            arrowInstance = existing.gameObject;
            arrowInstance.SetActive(false);
            arrowInstance.transform.SetParent(transform, true);
            arrowInstance.transform.localPosition = arrowLocalOffset;
            return;
        }

        if (arrowPrefab != null)
        {
            arrowInstance = Instantiate(arrowPrefab, transform);
            arrowInstance.name = "SelectionArrow";
            arrowInstance.transform.localPosition = arrowLocalOffset;
            arrowInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Mostrar / ocultar a seta de seleção (só afecta unidades com isPlayerUnit = true).
    /// </summary>
    public void ShowSelection(bool show)
    {
        if (!isPlayerUnit) return;

        if (arrowInstance != null)
            arrowInstance.SetActive(show);
    }

    void OnDestroy()
    {
        if (arrowInstance != null)
        {
            if (Application.isPlaying)
                Destroy(arrowInstance);
            else
                DestroyImmediate(arrowInstance);
        }
    }
}