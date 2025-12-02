using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Tooltip("Índice do nível (1-based)")]
    public int levelIndex = 1;

    [Header("UI References (opcionais)")]
    public GameObject lockedIcon;
    public GameObject completedIcon;
    public TextMeshProUGUI levelLabel;
    public TextMeshProUGUI difficultyLabel;

    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
        Refresh();
    }

    void OnEnable()
    {
        // Atualiza sempre que o painel for reativado
        Refresh();
    }

    public void Refresh()
    {
        if (LevelManager.Instance == null) return;

        bool unlocked = LevelManager.Instance.IsUnlocked(levelIndex);
        bool completed = LevelManager.Instance.IsCompleted(levelIndex);
        Difficulty diff = LevelManager.Instance.GetDifficulty(levelIndex);

        if (lockedIcon != null) lockedIcon.SetActive(!unlocked);
        if (completedIcon != null) completedIcon.SetActive(completed);
        if (levelLabel != null) levelLabel.text = $"Nível {levelIndex}";
        if (difficultyLabel != null) difficultyLabel.text = $"Dificuldade: {diff}";

        btn.interactable = unlocked;
    }

    void OnClick()
    {
        // Carrega o nível (poderias mostrar um painel de confirmação aqui)
        LevelManager.Instance.LoadLevel(levelIndex);
    }

    // Método público para ligar a um botão que altera a dificuldade
    public void CycleDifficulty()
    {
        if (LevelManager.Instance == null) return;
        Difficulty current = LevelManager.Instance.GetDifficulty(levelIndex);
        Difficulty next = (Difficulty)(((int)current + 1) % 3);
        LevelManager.Instance.SetDifficulty(levelIndex, next);
        Refresh();
    }
}