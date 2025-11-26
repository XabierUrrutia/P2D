using UnityEngine;

public class WinPanel : MonoBehaviour
{
    [Tooltip("Drag here the UI GameObject that represents the WIN screen (initially inactive)")]
    public GameObject winPanelUI;

    private bool _shown = false;

    void Start()
    {
        if (winPanelUI != null)
            winPanelUI.SetActive(false);
    }

    public void ShowWin()
    {
        if (_shown) return;
        _shown = true;

        if (winPanelUI != null)
            winPanelUI.SetActive(true);

        // Pausa o jogo
        Time.timeScale = 0f;

        Debug.Log("[WinPanel] Vitória - painel mostrado.");
    }

    public void HideWin()
    {
        if (!_shown) return;
        _shown = false;

        if (winPanelUI != null)
            winPanelUI.SetActive(false);

        Time.timeScale = 1f;
    }
}