using UnityEngine;
using TMPro; // se quiser usar TextMeshPro no painel

/// <summary>
/// Área de alerta separada da PlayerBase:
/// - Coloque este script num GameObject próprio (ex.: "BaseAlertZone").
/// - Esse GameObject deve ter um Collider2D com IsTrigger = true (CircleCollider2D recomendado)
///   e esse collider deve ser atribuído em detectionCollider.
/// - Quando um inimigo (tag enemyTag) entra na área, o painel warningPanel é mostrado (a piscar).
/// </summary>
[DisallowMultipleComponent]
public class PlayerBaseAlertZone : MonoBehaviour
{
    [Header("Configuração da Zona")]
    [Tooltip("Tag usada para identificar inimigos na cena.")]
    public string enemyTag = "Enemy";

    [Tooltip("Collider2D usado como área de detecção (deve estar com 'Is Trigger' ativado).")]
    public Collider2D detectionCollider;

    [Header("UI de Aviso")]
    [Tooltip("Painel de aviso a mostrar quando inimigos entram na zona (por exemplo, um painel com texto 'Inimigos perto da Base!').")]
    public GameObject warningPanel;

    [Tooltip("Texto opcional dentro do painel para mostrar a mensagem (pode ficar vazio).")]
    public TextMeshProUGUI warningText;

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

    private int enemiesInside;
    private float lastAlertSoundTime = -999f;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        // Se não foi atribuído no Inspector, tenta achar um Collider2D neste GameObject
        if (detectionCollider == null)
            detectionCollider = GetComponent<Collider2D>();

        // Se ainda não encontrou, tenta em filhos
        if (detectionCollider == null)
            detectionCollider = GetComponentInChildren<Collider2D>();

        if (detectionCollider == null)
        {
            Debug.LogError("[PlayerBaseAlertZone] Nenhum Collider2D atribuído/encontrado. " +
                           "Crie um GameObject de zona com CircleCollider2D (IsTrigger = true) " +
                           "e arraste para detectionCollider.");
            enabled = false;
            return;
        }

        if (!detectionCollider.isTrigger)
        {
            Debug.LogWarning("[PlayerBaseAlertZone] detectionCollider não está como Trigger. A definir 'isTrigger = true'.");
            detectionCollider.isTrigger = true;
        }

        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Só reagir se o trigger for o collider configurado
        if (other == null || other == detectionCollider)
            return;

        Debug.Log($"[PlayerBaseAlertZone] OnTriggerEnter2D com '{other.name}', tag='{other.tag}'");

        if (!other.CompareTag(enemyTag))
            return;

        enemiesInside++;
        Debug.Log($"[PlayerBaseAlertZone] Enemy entrou na zona. enemiesInside={enemiesInside}");

        if (enemiesInside == 1)
            ShowWarning();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || other == detectionCollider)
            return;

        Debug.Log($"[PlayerBaseAlertZone] OnTriggerExit2D com '{other.name}', tag='{other.tag}'");

        if (!other.CompareTag(enemyTag))
            return;

        enemiesInside = Mathf.Max(0, enemiesInside - 1);
        Debug.Log($"[PlayerBaseAlertZone] Enemy saiu da zona. enemiesInside={enemiesInside}");

        if (enemiesInside == 0)
            HideWarning();
    }

    void ShowWarning()
    {
        if (warningPanel != null)
        {
            // garante ligado inicialmente
            warningPanel.SetActive(true);

            var canvas = warningPanel.GetComponentInParent<Canvas>(true);
            if (canvas != null && !canvas.gameObject.activeInHierarchy)
                canvas.gameObject.SetActive(true);

            if (warningText != null)
                warningText.text = warningMessage;

            // iniciar piscar se configurado
            if (blinkWarning)
            {
                if (blinkCoroutine != null)
                    StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkWarningPanel());
            }
        }
        else
        {
            Debug.LogWarning("[PlayerBaseAlertZone] warningPanel não está atribuído no Inspector.");
        }

        if (playSoundOnFirstEnter &&
            Time.time - lastAlertSoundTime >= alertSoundCooldown &&
            SoundColector.Instance != null)
        {
            SoundColector.Instance.PlayInfantrySelect();
            lastAlertSoundTime = Time.time;
        }

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

        // pisca enquanto houver inimigos dentro
        while (enemiesInside > 0)
        {
            // toggle
            warningPanel.SetActive(!warningPanel.activeSelf);
            yield return new WaitForSeconds(Mathf.Max(0.05f, blinkInterval));
        }

        // ao sair do loop, garante que fica desligado
        warningPanel.SetActive(false);
        blinkCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = detectionCollider != null ? detectionCollider : GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = Color.red;
        var circle = col as CircleCollider2D;
        if (circle != null)
        {
            Vector3 center = circle.transform.TransformPoint(circle.offset);
            Gizmos.DrawWireSphere(center, circle.radius);
        }
        else
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}