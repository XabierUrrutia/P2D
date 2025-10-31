using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialStep2Manager : MonoBehaviour
{
    [Header("UI")]
    public GameObject stepCompletePanel;         // Painel que aparece quando matar o inimigo
    public Button proceedButton;                 // Botão para ir à cena seguinte (ou menu)
    public TextMeshProUGUI stepMessageText;      // Mensagem de confirmação
    public string nextSceneName = "";            // Cena seguinte (opcional)

    [Header("Enemy subscription")]
    public string enemyTag = "Enemy";            // Tag para procurar enemies na cena (pode especificar um inimigo manualmente)
    public GameObject specificEnemy;             // Se preenchido, o manager liga-se só a este inimigo

    void Start()
    {
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        // Se foi atribuída manualmente uma GameObject de inimigo, procura o componente EnemyDeathListener nele
        if (specificEnemy != null)
        {
            var listener = specificEnemy.GetComponent<EnemyDeathListener>();
            if (listener != null)
                listener.onEnemyDied.AddListener(OnEnemyDeath);
            else
                Debug.LogWarning($"TutorialStep2Manager: specificEnemy '{specificEnemy.name}' não tem EnemyDeathListener.");
        }
        else
        {
            // liga a todos os EnemyDeathListener presentes com a tag ou todos se tag vazia
            EnemyDeathListener[] listeners = FindObjectsOfType<EnemyDeathListener>();
            foreach (var l in listeners)
            {
                if (string.IsNullOrEmpty(enemyTag) || l.CompareTag(enemyTag) || (l.gameObject != null && l.gameObject.tag == enemyTag))
                {
                    l.onEnemyDied.AddListener(OnEnemyDeath);
                    Debug.Log($"[TutorialStep2Manager] subscrito a onEnemyDied de '{l.gameObject.name}'");
                }
            }

            if (listeners.Length == 0)
                Debug.LogWarning("TutorialStep2Manager: nenhum EnemyDeathListener encontrado na cena. Adiciona EnemyDeathListener ao prefab do inimigo.");
        }
    }

    void OnDestroy()
    {
        if (specificEnemy != null)
        {
            var listener = specificEnemy.GetComponent<EnemyDeathListener>();
            if (listener != null)
                listener.onEnemyDied.RemoveListener(OnEnemyDeath);
        }
        else
        {
            EnemyDeathListener[] listeners = FindObjectsOfType<EnemyDeathListener>();
            foreach (var l in listeners)
            {
                if (l != null)
                    l.onEnemyDied.RemoveListener(OnEnemyDeath);
            }
        }
    }

    // Chamado quando um EnemyDeathListener dispara
    public void OnEnemyDeath()
    {
        Debug.Log("[TutorialStep2Manager] OnEnemyDeath recebido");
        if (stepCompletePanel == null)
            Debug.LogWarning("[TutorialStep2Manager] stepCompletePanel NÃO está atribuído no Inspector!");
        ShowStepCompleteUI();
    }

    void ShowStepCompleteUI()
    {
        if (stepMessageText != null)
            stepMessageText.text = "Passaste o passo 2! Mataste o inimigo.";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            if (!string.IsNullOrEmpty(nextSceneName))
                proceedButton.onClick.AddListener(() => SceneManager.LoadScene(nextSceneName));
            proceedButton.interactable = true;
        }
    }

    public void SimulateEnemyDeath()
    {
        OnEnemyDeath();
    }
}