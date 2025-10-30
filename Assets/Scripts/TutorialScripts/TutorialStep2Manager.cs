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
                listener.OnDeath.AddListener(OnEnemyDeath);
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
                    l.OnDeath.AddListener(OnEnemyDeath);
                }
            }

            // Se não encontrou nenhum listener mas existem inimigos, avisa (útil para debug)
            if (listeners.Length == 0)
                Debug.LogWarning("TutorialStep2Manager: nenhum EnemyDeathListener encontrado na cena. Adiciona EnemyDeathListener ao prefab do inimigo.");
        }
    }

    // Chamado quando um EnemyDeathListener dispara
    public void OnEnemyDeath()
    {
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

    // Utilitário para testes
    public void SimulateEnemyDeath()
    {
        OnEnemyDeath();
    }
}