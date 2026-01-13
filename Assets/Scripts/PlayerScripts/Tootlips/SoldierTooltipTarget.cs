using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class SoldierTooltipTarget : MonoBehaviour
{
    [Header("Painel de info (local)")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoHpText;
    public TextMeshProUGUI infoXpText; // antes era Ammo

    [Header("Posicionamento relativo ao soldado")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Dados do soldado")]
    public string soldierTypeName = "Soldier";
    public MonoBehaviour healthComponent;

    private IHealth health;
    private Camera mainCamera;
    private RectTransform infoRect;

    // Referência à câmara para saber o nível de zoom
    private cameraFollow cameraFollow;

    // Apenas um painel ativo de cada vez
    private static SoldierTooltipTarget currentActive;

    // Referência à veterania/XP do soldado (mesmo tipo usado no HUD)
    public UnitVeterancy unitVeterancy; // arrasta o mesmo componente que o HUD usa

    void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
            cameraFollow = mainCamera.GetComponent<cameraFollow>();

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

        // Tentar encontrar UnitVeterancy automaticamente se não for atribuída
        if (unitVeterancy == null)
            unitVeterancy = GetComponent<UnitVeterancy>();

        if (unitVeterancy == null)
        {
            Debug.LogWarning($"[SoldierTooltipTarget] Nenhum UnitVeterancy encontrado em '{gameObject.name}'. XP/Nível vão mostrar valores vazios.");
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

            if (infoXpText == null)
            {
                var t = infoPanel.transform.Find("SoldierXPText");
                if (t != null)
                    infoXpText = t.GetComponent<TextMeshProUGUI>();
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
        if (infoPanel == null || infoRect == null || mainCamera == null)
            return;

        // Se o painel estiver ativo mas a câmara já não estiver no zoom mais perto, esconde.
        if (infoPanel.activeSelf && cameraFollow != null && !cameraFollow.IsAtClosestZoom())
        {
            infoPanel.SetActive(false);

            if (currentActive == this)
                currentActive = null;

            return;
        }

        if (!infoPanel.activeSelf)
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
            // Só mostra se o zoom estiver no nível mais perto
            if (cameraFollow != null && !cameraFollow.IsAtClosestZoom())
            {
                infoPanel.SetActive(false);
                return;
            }

            // Garante que só um painel está ativo
            if (currentActive != null && currentActive != this)
                currentActive.InternalHide();

            UpdateInfoPanel();
            infoPanel.SetActive(true);
            currentActive = this;
        }
        else
        {
            InternalHide();
        }
    }

    private void InternalHide()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (currentActive == this)
            currentActive = null;
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
                infoHpText.text = $"HP: {currentHp}/{maxHp}";
            else
                infoHpText.text = $"HP: {currentHp}";
        }

        // XP + Nível: usar os mesmos dados que o HUD (UnitVeterancy)
        if (infoXpText != null)
        {
            if (unitVeterancy != null)
            {
                float atual = unitVeterancy.xpActual;
                float necessario = unitVeterancy.xpParaSiguienteNivel;
                int nivel = unitVeterancy.nivel;

                infoXpText.text = $"LV {nivel}  XP: {atual}/{necessario}";
            }
            else
            {
                infoXpText.text = "LV -  XP: -";
            }
        }
    }
}