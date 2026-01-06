using UnityEngine;

[DisallowMultipleComponent]
public class SelectableUnit : MonoBehaviour
{
    public bool isPlayerUnit = true;
    public GameObject arrowPrefab;
    public Vector3 arrowLocalOffset = new Vector3(0f, 0f, 0f);

    private GameObject arrowInstance;
    private SoldierTooltipTarget tooltipTarget;
    private UnitVeterancy myVeterancy;

    void Awake()
    {
        myVeterancy = GetComponent<UnitVeterancy>();

        // --- DIAGNÓSTICO INICIAL ---
        if (myVeterancy == null) Debug.LogError($"[ERROR] Al soldado '{name}' le falta el script 'UnitVeterancy'. El HUD no funcionará.");
        // ---------------------------

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
        Debug.Log($"[SelectableUnit] ShowSelection llamado con valor: {show} en {name}");

        if (isPlayerUnit && arrowInstance != null) arrowInstance.SetActive(show);
        if (tooltipTarget != null) tooltipTarget.ShowInfo(show);

        if (show)
        {
            if (UnitHUDManager.Instance != null)
            {
                Debug.Log("[SelectableUnit] Enviando datos al HUD Manager...");
                UnitHUDManager.Instance.SeleccionarUnidad(myVeterancy);
            }
            else
            {
                Debug.LogError("[ERROR] No se encuentra 'UnitHUDManager'. ¿Está creado en la escena?");
            }
        }
    }

    // --- IMPORTANTE: ¿QUIÉN LLAMA A SHOWSELECTION? ---
    // Si no tienes otro script (como un SelectionManager) que llame a ShowSelection,
    // necesitas detectar el clic aquí mismo. Descomenta esto si no tienes sistema de selección:
    /*
    void OnMouseDown()
    {
        ShowSelection(true);
    }
    */

    void OnDestroy()
    {
        if (arrowInstance != null)
        {
            if (Application.isPlaying) Destroy(arrowInstance);
            else DestroyImmediate(arrowInstance);
        }
    }
}