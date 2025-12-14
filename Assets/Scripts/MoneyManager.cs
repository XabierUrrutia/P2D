using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Config")]
    public int startMoney = 10;

    [Header("Dinero del Jugador")]
    public int currentMoney = 0;

    public int CurrentMoney { get { return currentMoney; } }

    [Header("Rendimiento de edificios")]
    public float incomeTickInterval = 1f;

    private readonly List<BuildingOwnership> incomeSources = new List<BuildingOwnership>();
    private Coroutine incomeCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent != null)
                transform.SetParent(null);

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
        var hud = FindObjectOfType<MoneyTextHUD>();
        if (hud != null)
            hud.Refresh();
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
        return false;
    }

    public void ResetMoney()
    {
        currentMoney = startMoney;
    }

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
        if (incomeCoroutine != null)
            StopCoroutine(incomeCoroutine);
    }
}