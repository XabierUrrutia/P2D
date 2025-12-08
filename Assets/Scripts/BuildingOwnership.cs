using UnityEngine;

/// <summary>
/// Componente simples para controlar propriedade de um edifício e
/// registar/desregistrar a sua renda no MoneyManager.
/// Adicionar este componente aos prefabs de edifícios.
/// </summary>
public class BuildingOwnership : MonoBehaviour
{
    public enum Owner
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }

    [Header("Configuração de Renda")]
    [Tooltip("Quanto este edifício gera por tick (tick definido em MoneyManager.incomeTickInterval)")]
    public int incomePerTick = 1;

    [Tooltip("Identificador opcional do edifício")]
    public string buildingId;

    [Header("Estado")]
    public Owner owner = Owner.Neutral;

    void Start()
    {
        // caso já seja propriedade do jogador ao spawn, registar
        if (owner == Owner.Player)
            MoneyManager.Instance?.RegisterIncomeSource(this);
    }

    public void SetOwner(Owner newOwner)
    {
        if (owner == newOwner) return;

        // retirar registro anterior
        if (owner == Owner.Player)
            MoneyManager.Instance?.UnregisterIncomeSource(this);

        owner = newOwner;

        if (owner == Owner.Player)
        {
            MoneyManager.Instance?.RegisterIncomeSource(this);
            Debug.Log($"[Building] {name} agora pertence ao PLAYER. Renda: {incomePerTick}/tick");
        }
        else
        {
            Debug.Log($"[Building] {name} agora pertence a {owner}");
        }
    }

    // API de conveniência para captura por player
    public void CaptureByPlayer()
    {
        SetOwner(Owner.Player);
    }

    // API para perda de propriedade
    public void LoseOwnership()
    {
        SetOwner(Owner.Neutral);
    }

    void OnDestroy()
    {
        // limpar registro se necessário
        if (owner == Owner.Player)
            MoneyManager.Instance?.UnregisterIncomeSource(this);
    }
}