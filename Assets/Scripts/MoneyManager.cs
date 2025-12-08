using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Dinero del Jugador")]
    public int currentMoney = 0;

    // Propiedad pública para acceder al dinero desde otros scripts
    public int CurrentMoney { get { return currentMoney; } }

    [Header("UI Referencia")]
    public TextMeshProUGUI moneyText;

    [Header("Rendimiento de edificios")]
    [Tooltip("Intervalo (s) em que as receitas dos edificios são agregadas e aplicadas ao jogador")]
    public float incomeTickInterval = 1f;

    // fontes de rendimento (edifícios)
    private readonly List<BuildingOwnership> incomeSources = new List<BuildingOwnership>();
    private Coroutine incomeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Garantir que este GameObject é root antes de torná-lo persistente
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateMoneyUI();
        if (incomeCoroutine == null)
            incomeCoroutine = StartCoroutine(IncomeTickRoutine());
    }

    public void AddMoney(int amount)
    {
        if (amount == 0) return;
        currentMoney += amount;
        UpdateMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateMoneyUI();
            return true;
        }
        return false;
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{currentMoney}";
        }
    }

    // Registrar / Cancelar fontes de rendimento (chamado por BuildingOwnership)
    public void RegisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (!incomeSources.Contains(source))
            incomeSources.Add(source);
    }

    public void UnregisterIncomeSource(BuildingOwnership source)
    {
        if (source == null) return;
        if (incomeSources.Contains(source))
            incomeSources.Remove(source);
    }

    IEnumerator IncomeTickRoutine()
    {
        while (true)
        {
            if (incomeSources.Count > 0)
            {
                int total = 0;
                // soma rendimentos ativos
                for (int i = incomeSources.Count - 1; i >= 0; i--)
                {
                    var s = incomeSources[i];
                    if (s == null)
                    {
                        incomeSources.RemoveAt(i);
                        continue;
                    }
                    total += s.incomePerTick;
                }

                if (total != 0)
                    AddMoney(total);
            }

            yield return new WaitForSeconds(incomeTickInterval);
        }
    }

    void OnDestroy()
    {
        if (incomeCoroutine != null) StopCoroutine(incomeCoroutine);
    }
}