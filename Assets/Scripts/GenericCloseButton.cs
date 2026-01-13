using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script genérico para botões de "Close".
/// - Fecha um ou mais painéis/menus quando clicado.
/// - Pode ser usado para:
///   * fechar o UI da Player Base,
///   * fechar o Building Panel,
///   * fechar o Units/Soldiers Panel, etc.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class GenericCloseButton : MonoBehaviour
{
    [Header("Painéis a fechar")]
    [Tooltip("Lista de GameObjects (painéis/menus) que serão desativados quando clicar neste botão.")]
    public GameObject[] panelsToClose;

    [Header("Opções")]
    [Tooltip("Se true, reativa Time.timeScale = 1 ao fechar (útil se algum menu tiver pausado o jogo).")]
    public bool resumeTimeOnClose = false;

    private Button closeButton;

    void Awake()
    {
        closeButton = GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnCloseClicked()
    {
        SoundColector.Instance?.PlayUiClick();

        if (panelsToClose != null)
        {
            for (int i = 0; i < panelsToClose.Length; i++)
            {
                if (panelsToClose[i] != null)
                    panelsToClose[i].SetActive(false);
            }
        }

        if (resumeTimeOnClose)
        {
            Time.timeScale = 1f;
        }
    }
}