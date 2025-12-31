using UnityEngine;

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
    private SoldierTooltipTarget tooltipTarget;

    void Awake()
    {
        if (isPlayerUnit)
        {
            var existing = GetComponentInChildren<FloatingArrow>(true);
            if (existing != null)
            {
                arrowInstance = existing.gameObject;
                arrowInstance.SetActive(false);
                arrowInstance.transform.SetParent(transform, true);
                arrowInstance.transform.localPosition = arrowLocalOffset;
            }
            else if (arrowPrefab != null)
            {
                arrowInstance = Instantiate(arrowPrefab, transform);
                arrowInstance.name = "SelectionArrow";
                arrowInstance.transform.localPosition = arrowLocalOffset;
                arrowInstance.SetActive(false);
            }
        }

        tooltipTarget = GetComponent<SoldierTooltipTarget>();
    }

    public void ShowSelection(bool show)
    {
        if (isPlayerUnit && arrowInstance != null)
            arrowInstance.SetActive(show);

        if (tooltipTarget != null)
            tooltipTarget.ShowInfo(show);
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
