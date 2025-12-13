using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handler de clique para a PLAYER BASE.
/// - REQUER um Collider2D no GameObject da base para OnMouseDown funcionar.
/// - Abre um painel de UI específico da PlayerBase.
/// - Permite gastar dinheiro para curar a vida (HP) da PlayerBase.
/// - NÃO congela mais o jogo ao abrir o painel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PlayerBaseBuildingClickHandler1 : MonoBehaviour
{
    [Header("UI da Player Base")]
    [Tooltip("Painel de UI específico da PlayerBase (GameObject na cena)")]
    public GameObject panelPlayerBaseUI;

    [Tooltip("Texto que mostra a vida atual / máxima da base")]
    public TextMeshProUGUI hpText;

    [Tooltip("Slider que mostra visualmente a vida da base (opcional)")]
    public Slider hpSlider;

    [Header("Cura / Custo")]
    [Tooltip("HP curado por utilização do botão 'Heal' (fixo em 50 como pediste)")]
    public int healAmount = 50;

    [Tooltip("Custo em dinheiro por utilização de cura")]
    public int healCost = 20;

    [Tooltip("Botão da UI que executa a cura")]
    public Button healButton;

    private PlayerBase playerBase;
    private Collider2D buildingCollider;
    private bool clickWasOnThisBuilding;

    void Awake()
    {
        buildingCollider = GetComponent<Collider2D>();
        if (buildingCollider == null)
        {
            Debug.LogError($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' NÃO TEM Collider2D. Adicione um Collider2D para clique funcionar.");
            enabled = false;
            return;
        }

        if (buildingCollider.isTrigger)
        {
            Debug.LogWarning($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' tem Collider2D como Trigger. DESMARQUE 'Is Trigger' para OnMouseDown funcionar corretamente!");
        }

        playerBase = GetComponent<PlayerBase>();
        if (playerBase == null)
        {
            Debug.LogError($"[PlayerBaseBuildingClickHandler1] '{gameObject.name}' não tem componente PlayerBase. Este handler é só para a base do jogador.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // tentar encontrar painel automaticamente se não foi ligado no Inspector
        if (panelPlayerBaseUI == null)
        {
            var panel = FindObjectOfType<PanelPlayerBaseUI>(true);
            if (panel != null)
            {
                panelPlayerBaseUI = panel.gameObject;
                Debug.Log($"[PlayerBaseBuildingClickHandler1] PanelPlayerBaseUI encontrado automaticamente: {panelPlayerBaseUI.name}");
            }
        }

        if (panelPlayerBaseUI != null)
            panelPlayerBaseUI.SetActive(false);

        // Ligar botão de cura, se possível
        if (healButton == null && panelPlayerBaseUI != null)
        {
            // se tiver mais de um botão, recomendamos arrastar manualmente no Inspector
            healButton = panelPlayerBaseUI.GetComponentInChildren<Button>(true);
        }

        if (healButton != null)
        {
            healButton.onClick.RemoveAllListeners();
            healButton.onClick.AddListener(OnHealButtonClicked);
        }

        UpdateUI();
    }

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        clickWasOnThisBuilding = true;
        TogglePanel();
        clickWasOnThisBuilding = false;
    }

    public void TogglePanel()
    {
        if (!clickWasOnThisBuilding)
        {
            Debug.LogWarning("[PlayerBaseBuildingClickHandler1] TogglePanel() chamado sem clique direto na base. Ignorado por segurança.");
            return;
        }

        if (panelPlayerBaseUI == null)
        {
            Debug.LogError("[PlayerBaseBuildingClickHandler1] panelPlayerBaseUI não atribuído.");
            return;
        }

        bool open = !panelPlayerBaseUI.activeSelf;

        if (open)
        {
            panelPlayerBaseUI.SetActive(true);
            // REMOVIDO: Time.timeScale = 0f;  // não vamos mais congelar o jogo
            UpdateUI();
            Debug.Log("[PlayerBaseBuildingClickHandler1] Painel da PlayerBase ABERTO.");
        }
        else
        {
            panelPlayerBaseUI.SetActive(false);
            // REMOVIDO: Time.timeScale = 1f;
            Debug.Log("[PlayerBaseBuildingClickHandler1] Painel da PlayerBase FECHADO.");
        }
    }

    void OnHealButtonClicked()
    {
        if (playerBase == null)
            return;

        // ver se há HP a curar
        if (playerBase.GetCurrentHealth() >= playerBase.GetMaxHealth())
        {
            Debug.Log("[PlayerBaseBuildingClickHandler1] Base já está com HP máximo. Cura ignorada.");
            UpdateUI();
            return;
        }

        // verificar dinheiro suficiente
        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[PlayerBaseBuildingClickHandler1] MoneyManager.Instance é null. Não é possível curar.");
            return;
        }

        if (MoneyManager.Instance.CurrentMoney < healCost)
        {
            Debug.Log("[PlayerBaseBuildingClickHandler1] Dinheiro insuficiente para curar a base.");
            UpdateUI();
            return;
        }

        // gastar dinheiro e curar (NÃO cria prefab nenhum, só altera HP)
        MoneyManager.Instance.SpendMoney(healCost);
        playerBase.Heal(healAmount); // cura 50 HP como pediste

        Debug.Log($"[PlayerBaseBuildingClickHandler1] Base curada em {healAmount} HP por {healCost} moedas.");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerBase == null)
            return;

        int hp = playerBase.GetCurrentHealth();
        int maxHp = playerBase.GetMaxHealth();

        if (hpText != null)
            hpText.text = $"{hp} / {maxHp}";

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = hp;
        }

        if (healButton != null)
        {
            bool canHeal =
                hp < maxHp &&
                MoneyManager.Instance != null &&
                MoneyManager.Instance.CurrentMoney >= healCost;

            healButton.interactable = canHeal;
        }
    }

    public void RefreshUI()
    {
        UpdateUI();
    }

    void OnDisable()
    {
        if (panelPlayerBaseUI != null && panelPlayerBaseUI.activeSelf)
        {
            panelPlayerBaseUI.SetActive(false);
            // REMOVIDO: Time.timeScale = 1f;
        }
    }
}