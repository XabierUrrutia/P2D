using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("�ndice do passo que este trigger completa (0-based)")]
    public int stepIndex = 0;

    [Tooltip("Se true, o trigger s� funciona uma vez")]
    public bool singleUse = true;

    [Header("Eventos do Inspector")]
    [Tooltip("Eventos a executar quando o jogador entra no trigger (pode atribuir m�todos no Inspector)")]
    public UnityEvent onPlayerEnter;

    [Header("Detec��o por Tag")]
    [Tooltip("Tag que identifica o jogador (usado na compara��o direta)")]
    public string playerTag = "Player";

    [Tooltip("Se preenchido, o GameObject do trigger (ex.: a torre) deve ter esta tag para o trigger funcionar. Deixa vazio para ignorar.")]
    public string requiredThisTag = "Tower";

    [Tooltip("Se true, aceita colisores filhos do jogador (procura tag no parent/root)")]
    public bool allowParentTagCheck = true;

    [Header("Fallbacks (opcionais)")]
    [Tooltip("Se true, aceita objectos que tenham PlayerHealth em qualquer ancestor (�til quando coliders est�o em filhos)")]
    public bool allowPlayerHealthCheck = true;

    [Header("Auto-corre��es")]
    [Tooltip("Se true, se o jogador n�o tiver Rigidbody2D o script adiciona um Rigidbody2D kinematic automaticamente (�til em prot�tipo).")]
    public bool autoAddRigidbodyToPlayer = true;

    [Header("Mensagem de passo")]
    [Tooltip("Texto TMP que exibir� a mensagem 'Passaste o passo X' (opcional)")]
    public TextMeshProUGUI stepMessageText;
    [Tooltip("Dura��o em segundos da mensagem na tela")]
    public float messageDuration = 2f;

    void Reset()
    {
        // garante que � trigger no editor ao adicionar
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
            Debug.LogError($"TutorialTrigger precisa de um Collider2D em '{name}'");
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"TutorialTrigger '{name}': Collider2D n�o estava em 'Is Trigger'. Foi ativado automaticamente.");
        }

        // Aviso �til se requiredThisTag estiver preenchido mas o objeto n�o tiver a tag
        if (!string.IsNullOrEmpty(requiredThisTag) && !gameObject.CompareTag(requiredThisTag))
        {
            Debug.LogWarning($"TutorialTrigger '{name}': expected tag '{requiredThisTag}' on this GameObject but actual tag is '{gameObject.tag}'. Ajusta no Inspector ou coloca a tag '{requiredThisTag}' no objecto.");
        }

        // Verifica��o e corre��o comum: existe o jogador com tag? tem Collider2D e Rigidbody2D?
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogWarning($"TutorialTrigger '{name}': n�o foi encontrado nenhum GameObject com a tag '{playerTag}'. Verifica a tag do jogador.");
            }
            else
            {
                Collider2D playerCol = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>();
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInChildren<Rigidbody2D>();

                if (playerCol == null)
                {
                    Debug.LogWarning($"TutorialTrigger '{name}': o jogador '{player.name}' n�o parece ter um Collider2D. Adiciona um Collider2D (ex.: CircleCollider2D) ao jogador para que triggers funcionem.");
                }

                if (playerRb == null)
                {
                    if (autoAddRigidbodyToPlayer && playerCol != null)
                    {
                        // Adiciona Rigidbody2D e configura como kinematic para n�o interferir com movement code baseado em transform
                        var rb = player.AddComponent<Rigidbody2D>();
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        rb.simulated = true;
                        Debug.Log($"TutorialTrigger '{name}': adicionou Rigidbody2D kinematic tempor�rio ao jogador '{player.name}' porque estava ausente. Para produ��o, adiciona um Rigidbody2D no prefab do jogador.");
                    }
                    else
                    {
                        Debug.LogWarning($"TutorialTrigger '{name}': o jogador '{player.name}' n�o tem Rigidbody2D. Pelo menos um dos dois colliders (trigger ou jogador) deve ter Rigidbody2D para que OnTrigger funcione.");
                    }
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnter(other.gameObject);
    }

    // fallback caso o collider n�o esteja como trigger (opcional)
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleEnter(collision.gameObject);
    }

    // M�todo p�blico para testar via Inspector ou bot�o
    public void SimulateEnter()
    {
        Debug.Log($"TutorialTrigger '{name}': SimulateEnter chamado para step {stepIndex}");
        StartCoroutine(ShowStepMessageCoroutine());
        InvokeActions();
    }

    bool IsPlayerByTag(GameObject go)
    {
        if (go == null) return false;

        // Tag direta
        if (!string.IsNullOrEmpty(playerTag) && go.CompareTag(playerTag))
            return true;

        // Tag em ancestor/root
        if (allowParentTagCheck)
        {
            Transform t = go.transform;
            while (t != null)
            {
                if (!string.IsNullOrEmpty(playerTag) && t.gameObject.CompareTag(playerTag))
                    return true;
                t = t.parent;
            }
        }

        return false;
    }

    bool IsPlayerByFallbacks(GameObject go)
    {
        if (go == null) return false;

        if (allowPlayerHealthCheck && go.GetComponentInParent<PlayerHealth>() != null)
            return true;

        return false;
    }

    void HandleEnter(GameObject otherGO)
    {
        if (otherGO == null) return;

        // DEBUG: imprime infos �teis para diagnosticar
        Debug.Log($"TutorialTrigger '{name}': OnEnter detectado por '{otherGO.name}'. Tag: '{otherGO.tag}'. Root Tag: '{otherGO.transform.root.tag}'. " +
                  $"HasPlayerHealthParent={(otherGO.GetComponentInParent<PlayerHealth>() != null)} HasRigidbody={(otherGO.GetComponent<Rigidbody2D>() != null)}");

        // Se requisitado, valida tag do pr�prio trigger (ex.: "Tower")
        if (!string.IsNullOrEmpty(requiredThisTag) && !gameObject.CompareTag(requiredThisTag))
        {
            Debug.Log($"TutorialTrigger '{name}': Trigger n�o tem a tag requerida '{requiredThisTag}'. Ignorando.");
            return;
        }

        // Primeiro tentativa: identifica��o por tags (priorit�ria)
        if (IsPlayerByTag(otherGO))
        {
            TriggerActivated(otherGO);
            return;
        }

        // Se tag n�o detectada, tenta fallbacks opcionais (componentes)
        if (IsPlayerByFallbacks(otherGO))
        {
            TriggerActivated(otherGO);
            return;
        }

        Debug.Log($"TutorialTrigger '{name}': '{otherGO.name}' n�o identificado como jogador (tag '{playerTag}' ausente e fallbacks falharam). Ignorando.");
    }

    void TriggerActivated(GameObject playerGO)
    {
        // Invoca callbacks do Inspector � managers por cena devem subscrever aqui
        if (onPlayerEnter != null)
            onPlayerEnter.Invoke();

        // Mostra na tela a mensagem de passo conclu�do (se estiver configurado)
        StartCoroutine(ShowStepMessageCoroutine());

        if (singleUse)
            gameObject.SetActive(false);
    }

    void InvokeActions()
    {
        if (onPlayerEnter != null)
            onPlayerEnter.Invoke();
    }

    IEnumerator ShowStepMessageCoroutine()
    {
        if (stepMessageText == null) yield break;

        string original = stepMessageText.text;
        stepMessageText.text = $"Passaste o passo {stepIndex + 1}!";
        stepMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        stepMessageText.text = original;
        // opcional: esconder o campo ap�s mensagem
        // stepMessageText.gameObject.SetActive(false);
    }
}