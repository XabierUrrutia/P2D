using UnityEngine;
using TMPro; // se quiser usar TextMeshPro no painel

/// <summary>
/// Zona de alerta da PlayerBase baseada em raio (usa Physics2D.OverlapCircle):
/// - Pode ser colocada num GameObject próprio (ex.: "BaseAlertZone") filho da PlayerBase.
/// - Não precisa de Collider2D: usa visionRadius para detetar inimigos na área.
/// - Pode opcionalmente sincronizar visionRadius com um FogStaticVision no mesmo GameObject.
/// - Quando um inimigo (tag enemyTag) entra na área, o warningPanel é mostrado (a piscar).
/// </summary>
[DisallowMultipleComponent]
public class PlayerBaseAlertZone : MonoBehaviour
{
    [Header("Configuração da Zona")]
    [Tooltip("Tag usada para identificar inimigos na cena.")]
    public string enemyTag = "Enemy";

    [Tooltip("Raio de detecção da zona de alerta (em unidades do mundo).")]
    public float alertRadius = 10f;

    [Tooltip("Se true, tenta copiar o radius de um FogStaticVision no mesmo GameObject.")]
    public bool syncWithFogStaticVision = true;

    [Header("UI de Aviso (pode ser encontrado por nome)")]
    [Tooltip("Painel de aviso a mostrar quando inimigos entram na zona. Se vazio, será procurado por nome em warningPanelName.")]
    public GameObject warningPanel;

    [Tooltip("Texto opcional dentro do painel para mostrar a mensagem. Se vazio, será procurado por nome em warningTextName.")]
    public TextMeshProUGUI warningText;

    [Tooltip("Nome do GameObject do painel, usado se warningPanel não for atribuído no Inspector.")]
    public string warningPanelName = "BaseWarningPanel";

    [Tooltip("Nome do GameObject do texto (TextMeshProUGUI), usado se warningText não for atribuído.")]
    public string warningTextName = "BaseWarningText";

    [Tooltip("Mensagem a mostrar quando inimigos são detectados.")]
    public string warningMessage = "Inimigos perto da Base!";

    [Header("Animação de Piscar")]
    [Tooltip("Se true, o painel vai piscar enquanto houver inimigos na zona.")]
    public bool blinkWarning = true;

    [Tooltip("Intervalo de piscar em segundos (tempo entre ligado/desligado).")]
    public float blinkInterval = 0.4f;

    [Tooltip("Se true, tocará um som de alerta quando o primeiro inimigo entrar.")]
    public bool playSoundOnFirstEnter = true;

    [Tooltip("Evitar spam de som de alerta (segundos entre alertas).")]
    public float alertSoundCooldown = 3f;

    [Header("Performance")]
    [Tooltip("Intervalo entre verificações (segundos). Valores como 0.2–0.5 são razoáveis).")]
    public float checkInterval = 0.3f;

    private float lastCheckTime;
    private int enemiesInside;
    private float lastAlertSoundTime = -999f;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        // Se existir FogStaticVision, copiar visionRadius
        if (syncWithFogStaticVision)
        {
            var fogStatic = GetComponent<FogStaticVision>();
            if (fogStatic != null)
            {
                alertRadius = fogStatic.visionRadius;
                Debug.Log($"[PlayerBaseAlertZone] alertRadius sincronizado com FogStaticVision: {alertRadius}");
            }
        }

        EnsureWarningUIReferences();

        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    void EnsureWarningUIReferences()
    {
        // Painel
        if (warningPanel == null && !string.IsNullOrEmpty(warningPanelName))
        {
            var panelObj = GameObject.Find(warningPanelName);
            if (panelObj != null)
            {
                warningPanel = panelObj;
                Debug.Log($"[PlayerBaseAlertZone] warningPanel encontrado por nome: {warningPanelName}");
            }
            else
            {
                Debug.LogWarning($"[PlayerBaseAlertZone] Não foi encontrado nenhum GameObject com nome '{warningPanelName}' para warningPanel.");
            }
        }

        // Texto
        if (warningText == null && !string.IsNullOrEmpty(warningTextName))
        {
            var textObj = GameObject.Find(warningTextName);
            if (textObj != null)
            {
                warningText = textObj.GetComponent<TextMeshProUGUI>();
                if (warningText != null)
                {
                    Debug.Log($"[PlayerBaseAlertZone] warningText encontrado por nome: {warningTextName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerBaseAlertZone] GameObject '{warningTextName}' encontrado, mas não tem TextMeshProUGUI.");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerBaseAlertZone] Não foi encontrado nenhum GameObject com nome '{warningTextName}' para warningText.");
            }
        }
    }

    void Update()
    {
        if (Time.time < lastCheckTime + checkInterval)
            return;

        lastCheckTime = Time.time;
        CheckEnemiesInRadius();
    }

    void CheckEnemiesInRadius()
    {
        // OverlapCircle ao redor deste GameObject (usa posição do alerta, normalmente filho da base)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, alertRadius);

        int count = 0;

        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.CompareTag(enemyTag))
                count++;
        }

        if (count > 0 && enemiesInside == 0)
        {
            enemiesInside = count;
            ShowWarning();
        }
        else if (count > 0 && enemiesInside > 0)
        {
            // ainda há inimigos, apenas atualiza contador
            enemiesInside = count;
        }
        else if (count == 0 && enemiesInside > 0)
        {
            enemiesInside = 0;
            HideWarning();
        }
    }

    void ShowWarning()
    {
        EnsureWarningUIReferences();

        if (warningPanel != null)
        {
            warningPanel.SetActive(true);

            var canvas = warningPanel.GetComponentInParent<Canvas>(true);
            if (canvas != null && !canvas.gameObject.activeInHierarchy)
                canvas.gameObject.SetActive(true);

            if (warningText != null)
                warningText.text = warningMessage;

            if (blinkWarning)
            {
                if (blinkCoroutine != null)
                    StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkWarningPanel());
            }
        }
        else
        {
            Debug.LogWarning("[PlayerBaseAlertZone] warningPanel não está atribuído nem encontrado por nome.");
        }
        /*
        if (playSoundOnFirstEnter &&
            Time.time - lastAlertSoundTime >= alertSoundCooldown &&
            SoundColector.Instance != null)
        {
            SoundColector.Instance.PlayInfantrySelect();
            lastAlertSoundTime = Time.time;
        }*/

        Debug.Log("[PlayerBaseAlertZone] Inimigos DETECTADOS perto da base!");
    }

    void HideWarning()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (warningPanel != null)
            warningPanel.SetActive(false);

        Debug.Log("[PlayerBaseAlertZone] Nenhum inimigo perto da base.");
    }

    System.Collections.IEnumerator BlinkWarningPanel()
    {
        if (warningPanel == null)
            yield break;

        while (enemiesInside > 0)
        {
            warningPanel.SetActive(!warningPanel.activeSelf);
            yield return new WaitForSeconds(Mathf.Max(0.05f, blinkInterval));
        }

        warningPanel.SetActive(false);
        blinkCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}