using UnityEngine;
using TMPro;

/// <summary>
/// Script para ser colocado diretamente no GameObject do texto de dinheiro (MoneyText).
/// - Liga-se ao MoneyManager.Instance quando a cena começa.
/// - Atualiza o texto imediatamente com o valor atual.
/// - Opcionalmente pode atualizar em intervalo regular.
/// </summary>
[DisallowMultipleComponent]
public class MoneyTextHUD : MonoBehaviour
{
    [Tooltip("Referência explícita ao TextMeshProUGUI (se vazio, será obtida no próprio GameObject).")]
    public TextMeshProUGUI moneyText;

    [Tooltip("Atualizar a cada frame? Se false, atualiza apenas em Start e quando chamar Refresh().")]
    public bool updateEveryFrame = false;

    void Awake()
    {
        if (moneyText == null)
        {
            moneyText = GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if (updateEveryFrame)
        {
            Refresh();
        }
    }

    /// <summary>
    /// Atualiza o texto com o dinheiro atual do MoneyManager.
    /// Pode ser chamado por outros scripts quando o dinheiro mudar.
    /// </summary>
    public void Refresh()
    {
        if (moneyText == null)
            return;

        if (MoneyManager.Instance == null)
        {
            moneyText.text = "0";
            return;
        }

        moneyText.text = $"{MoneyManager.Instance.CurrentMoney}";
    }
}