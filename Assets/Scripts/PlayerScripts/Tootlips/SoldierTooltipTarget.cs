using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class SoldierTooltipTarget : MonoBehaviour
{
    [Header("Painel de info (local)")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoHpText;
    public TextMeshProUGUI infoAmmoText;

    [Header("Posicionamento relativo ao soldado")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Dados do soldado")]
    public string soldierTypeName = "Soldier";
    public MonoBehaviour healthComponent;

    private IHealth health;
    private Camera mainCamera;
    private RectTransform infoRect;

    void Awake()
    {
        mainCamera = Camera.main;

        if (healthComponent == null)
            healthComponent = GetComponent<MonoBehaviour>();

        if (healthComponent is IHealth h)
            health = h;
        else
            health = GetComponent<IHealth>();

        if (health == null)
        {
            Debug.LogWarning($"[SoldierTooltipTarget] Nenhum IHealth encontrado em '{gameObject.name}'. Painel vai mostrar só o tipo.");
        }

        if (infoPanel == null)
        {
            Transform t = transform.Find("SoldierInfoPanel");
            if (t != null)
                infoPanel = t.gameObject;
        }

        if (infoPanel != null)
        {
            infoRect = infoPanel.GetComponent<RectTransform>();

            if (infoNameText == null)
            {
                var t = infoPanel.transform.Find("SoldierNameText");
                if (t != null)
                    infoNameText = t.GetComponent<TextMeshProUGUI>();
            }

            if (infoHpText == null)
            {
                var t = infoPanel.transform.Find("SoldierHPText");
                if (t != null)
                    infoHpText = t.GetComponent<TextMeshProUGUI>();
            }

            if (infoAmmoText == null)
            {
                var t = infoPanel.transform.Find("SoldierAmmoText");
                if (t != null)
                    infoAmmoText = t.GetComponent<TextMeshProUGUI>();
            }

            infoPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[SoldierTooltipTarget] infoPanel não atribuído/não encontrado em '{gameObject.name}'.");
        }
    }

    void Update()
    {
        if (infoPanel == null || !infoPanel.activeSelf || infoRect == null || mainCamera == null)
            return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position + worldOffset);
        infoRect.position = screenPos;
    }

    public void ShowInfo(bool show)
    {
        if (infoPanel == null)
            return;

        Debug.Log($"[SoldierTooltipTarget] ShowInfo({show}) em '{gameObject.name}'");

        if (show)
        {
            UpdateInfoPanel();
            infoPanel.SetActive(true);
        }
        else
        {
            infoPanel.SetActive(false);
        }
    }

    private void UpdateInfoPanel()
    {
        if (infoNameText != null)
            infoNameText.text = soldierTypeName;

        if (infoHpText != null)
        {
            int currentHp = 0;
            int maxHp = 0;

            if (health != null)
            {
                currentHp = health.GetCurrentHealth();
                maxHp = health.GetMaxHealth();
            }

            if (maxHp > 0)
                infoHpText.text = $"HP: {currentHp} / {maxHp}";
            else
                infoHpText.text = $"HP: {currentHp}";
        }

        if (infoAmmoText != null)
        {
            infoAmmoText.text = "Ammo: -";
        }
    }
}