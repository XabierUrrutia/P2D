using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialStep1Manager : MonoBehaviour
{
    [Header("UI")]
    public GameObject stepCompletePanel;          // Painel que aparece quando passar o passo
    public Button proceedButton;                  // Bot�o para ir � pr�xima cena
    public TextMeshProUGUI stepMessageText;       // Mensagem que mostra "Passaste o passo X"
    public string nextSceneName = "";             // Nome da cena do passo 2

    [Header("Trigger auto-bind")]
    public int triggerStepIndex = 0;              // �ndice do trigger que corresponde a este passo (normalmente 0)

    void Start()
    {
        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(false);

        // Tenta ligar automaticamente a qualquer TutorialTrigger com o �ndice correto
        var triggers = FindObjectsOfType<TutorialTrigger>();
        foreach (var t in triggers)
        {
            if (t.stepIndex == triggerStepIndex)
            {
                // adiciona listener para quando o trigger for ativado
                t.onPlayerEnter.AddListener(OnPlayerReachedTower);
            }
        }
    }

    // M�todo p�blico para conectar manualmente ao TutorialTrigger.onPlayerEnter (op��o)
    public void OnPlayerReachedTower()
    {
        ShowStepCompleteUI();
    }

    void ShowStepCompleteUI()
    {
        if (stepMessageText != null)
            stepMessageText.text = "Passaste o passo 1! Vai para o passo 2.";

        if (stepCompletePanel != null)
            stepCompletePanel.SetActive(true);

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(ProceedToNextScene);
            proceedButton.interactable = true;
        }
    }

    void ProceedToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("TutorialStep1Manager: nextSceneName n�o definido.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // Utilit�rio para testes manuais via Inspector
    public void SimulateComplete()
    {
        OnPlayerReachedTower();
    }
}