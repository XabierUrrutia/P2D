using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Inflación de Salarios")]
    public float costoMultiplicador = 1.0f;

    [Header("Configuración Económica")]
    public bool sePaganSalarios = false;

    [Header("Config")]
    public int startMoney = 10;

    [Header("Dinero del Jugador")]
    public int currentMoney = 0;
    public int CurrentMoney { get { return currentMoney; } }

    [Header("Rendimiento")]
    public float incomeTickInterval = 1f;

    // Lista de Edificios (Ingresos)
    private readonly List<BuildingOwnership> incomeSources = new List<BuildingOwnership>();

    // --- NUEVO: Lista de Unidades (Gastos) ---
    private readonly List<UnitVeterancy> activeUnits = new List<UnitVeterancy>();

    private Coroutine incomeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentMoney = startMoney;
    }

    void Start()
    {
        if (incomeCoroutine == null)
            incomeCoroutine = StartCoroutine(IncomeTickRoutine());
    }

    private void NotifyHUD()
    {
        var hud = FindObjectOfType<MoneyTextHUD>(); // Asegúrate de que este script exista
        if (hud != null) hud.Refresh();
    }

    public void AddMoney(int amount)
    {
        if (amount == 0) return;
        currentMoney += amount;
        NotifyHUD();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            NotifyHUD();
            return true;
        }
        GameEvents.RaiseInsufficientResources();
        return false;
    }

    public void ResetMoney()
    {
        currentMoney = startMoney;
        activeUnits.Clear(); // Limpiamos listas al reiniciar
        incomeSources.Clear();
    }

    // --- REGISTRO DE EDIFICIOS ---
    public void RegisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (!incomeSources.Contains(source)) incomeSources.Add(source);
    }

    public void UnregisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (incomeSources.Contains(source)) incomeSources.Remove(source);
    }

    // --- NUEVO: REGISTRO DE UNIDADES ---
    public void RegisterUnitExpense(UnitVeterancy unit)
    {
        if (unit == null) return;
        if (!activeUnits.Contains(unit)) activeUnits.Add(unit);
    }

    public void UnregisterUnitExpense(UnitVeterancy unit)
    {
        if (unit == null) return;
        if (activeUnits.Contains(unit)) activeUnits.Remove(unit);
    }

    // --- RUTINA PRINCIPAL DE ECONOMÍA ---
    IEnumerator IncomeTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(incomeTickInterval);

            // 1. CALCULAR INGRESOS
            int totalIncome = 0;
            for (int i = incomeSources.Count - 1; i >= 0; i--)
            {
                if (incomeSources[i] == null) incomeSources.RemoveAt(i);
                else totalIncome += incomeSources[i].incomePerTick;
            }

            // 2. CALCULAR GASTOS (MANTENIMIENTO)
            int totalUpkeep = 0;

            // --- NUEVO: SOLO CALCULAMOS GASTOS SI YA SE ACTIVARON LOS SALARIOS ---
            if (sePaganSalarios)
            {
                for (int i = activeUnits.Count - 1; i >= 0; i--)
                {
                    if (activeUnits[i] == null) activeUnits.RemoveAt(i);
                    else totalUpkeep += activeUnits[i].CalcularMantenimiento();
                }
            }
            // ---------------------------------------------------------------------

            // 3. APLICAR BALANCE
            currentMoney += totalIncome;

            if (currentMoney >= totalUpkeep)
            {
                currentMoney -= totalUpkeep;
            }
            else
            {
                currentMoney = 0;
            }

            NotifyHUD();
        }
    }

    void OnDestroy()
    {
        if (incomeCoroutine != null) StopCoroutine(incomeCoroutine);
    }

    // Método auxiliar por si quieres mostrar el gasto total en el UI
    public int CalculateTotalUpkeep()
    {
        int total = 0;
        foreach (var u in activeUnits)
        {
            if (u != null) total += u.CalcularMantenimiento();
        }
        return total;
    }

    public void ModificarInflacion(float cantidad)
    {
        costoMultiplicador += cantidad;
        Debug.Log($"Inflación aumentada en +{cantidad}. Nuevo multiplicador: x{costoMultiplicador}");
    }

    public void ActivarCobroDeSalarios()
    {
        if (!sePaganSalarios)
        {
            sePaganSalarios = true;
            Debug.Log("¡Se ha conquistado la primera fábrica! Los soldados ahora exigen su sueldo.");
        }
    }
}