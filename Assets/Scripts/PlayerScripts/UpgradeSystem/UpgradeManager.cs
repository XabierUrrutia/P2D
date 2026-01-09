using UnityEngine;
using System;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    // Evento que avisa a todas las unidades cuando compramos una mejora
    public event Action OnUpgradesChanged;

    [Header("Configuración de Mejoras")]
    public int damageBonusPerLevel = 1;
    public float speedBonusPerLevel = 0.5f;
    public int healthBonusPerLevel = 2;
    public float visionBonusPerLevel = 2f;

    [Header("Niveles Actuales (Solo lectura)")]
    public int currentDamageLevel = 0;
    public int currentSpeedLevel = 0;
    public int currentHealthLevel = 0;
    public int currentVisionLevel = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- MÉTODOS PARA COMPRAR MEJORAS (Llámalos desde botones UI) ---

    public void UpgradeDamage()
    {
        // Aquí podrías comprobar si tienes dinero/recursos antes
        currentDamageLevel++;
        NotifyUnits();
        Debug.Log("¡Daño mejorado al nivel " + currentDamageLevel + "!");
    }

    public void UpgradeSpeed()
    {
        currentSpeedLevel++;
        NotifyUnits();
        Debug.Log("¡Velocidad mejorada al nivel " + currentSpeedLevel + "!");
    }

    public void UpgradeHealth()
    {
        currentHealthLevel++;
        NotifyUnits();
        Debug.Log("¡Salud mejorada al nivel " + currentHealthLevel + "!");
    }

    public void UpgradeVision()
    {
        currentVisionLevel++;
        NotifyUnits();
        Debug.Log("¡Visión mejorada al nivel " + currentVisionLevel + "!");
    }

    private void NotifyUnits()
    {
        // Dispara el evento para que todas las unidades vivas se actualicen
        OnUpgradesChanged?.Invoke();
    }

    // --- MÉTODOS PARA OBTENER EL VALOR ACTUAL ---

    public int GetTotalDamageBonus() => currentDamageLevel * damageBonusPerLevel;
    public float GetTotalSpeedBonus() => currentSpeedLevel * speedBonusPerLevel;
    public int GetTotalHealthBonus() => currentHealthLevel * healthBonusPerLevel;
    public float GetTotalVisionBonus() => currentVisionLevel * visionBonusPerLevel;
}