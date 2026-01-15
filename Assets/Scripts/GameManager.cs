using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // --- NUEVO: CONFIGURACIÓN DE DERROTA ---
    [Header("Condiciones de Derrota")]
    public int costeMinimoParaJugar = 150; // El coste de tu unidad más barata
    public bool baseDestruida = false;
    // ---------------------------------------

    private List<IHealth> allUnits = new List<IHealth>();
    private bool gameOver = false;

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
        }
    }

    void Start()
    {
        // Comprobar nada más empezar por si la configuración inicial ya es de derrota
        Invoke("CheckGameOver", 0.5f);
    }

    public void RegisterUnit(IHealth unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            Debug.Log($"Unidad registrada: {unit.transform.name}. Total: {allUnits.Count}");
        }
    }

    public void UnregisterUnit(IHealth unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            Debug.Log($"Unidad desregistrada: {unit.transform.name}. Total: {allUnits.Count}");

            // Verificar si todas las unidades han muerto
            CheckGameOver();
        }
    }

    public void AddNewUnit(IHealth newUnit)
    {
        RegisterUnit(newUnit);
    }

    // --- NUEVO: MÉTODO PARA CUANDO DESTRUYEN LA BASE ---
    public void NotificarBaseDestruida()
    {
        baseDestruida = true;
        CheckGameOver(); // Forzamos la comprobación inmediatamente
    }
    // ---------------------------------------------------

    // --- NUEVO: MÉTODO PARA VERIFICAR DINERO (Llamado desde MoneyManager) ---
    public void VerificarDineroTrasGasto()
    {
        CheckGameOver();
    }
    // ------------------------------------------------------------------------

    private void CheckGameOver()
    {
        // 1. Limpieza de lista (borrar muertos/nulos)
        allUnits.RemoveAll(unit => unit == null || unit.IsDead);

        if (gameOver) return;

        // 2. SI LA BASE CAYÓ -> FIN DIRECTO
        if (baseDestruida)
        {
            Debug.Log("¡Base Principal destruida! Fin del juego.");
            TriggerGameOver();
            return;
        }

        // 3. CONTEO DISCRIMINADO
        // Contamos solo las unidades que NO sean edificios.
        int tropasDeCombate = 0;

        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
            {
                // TRUCO: Accedemos al transform para buscar el script marcador
                // Si NO tiene el script "EsEdificio", entonces es un soldado/tanque.
                if (unit.transform.GetComponent<EsEdificio>() == null)
                {
                    tropasDeCombate++;
                }
            }
        }

        // Consultamos el dinero
        int dineroActual = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        // Debug para verificar (opcional)
        // Debug.Log($"Revisión -> Tropas Móviles: {tropasDeCombate} | Dinero: {dineroActual}");

        // 4. CONDICIÓN DE DERROTA
        // Si no quedan tropas de combate (aunque tengas 50 torretas) y no hay dinero...
        if (tropasDeCombate == 0 && dineroActual < costeMinimoParaJugar)
        {
            Debug.Log("GAME OVER: Solo quedan edificios y no hay dinero para reclutar.");
            TriggerGameOver();
        }
    }

    // He separado la lógica de activar el fin del juego para reutilizarla
    private void TriggerGameOver()
    {
        gameOver = true;
        Invoke("LoadGameOverScene", 1f);
    }

    public int GetActiveUnitsCount()
    {
        int aliveCount = 0;
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
                aliveCount++;
        }
        return aliveCount;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    private void LoadGameOverScene()
    {
        SoundColector.Instance?.PlayDefeatMusic();
        SceneManager.LoadScene(9);
    }

    public void ResetGame()
    {
        allUnits.Clear();
        gameOver = false;
        baseDestruida = false; // Reseteamos también esto
    }

    public List<IHealth> GetAllUnits()
    {
        return new List<IHealth>(allUnits);
    }

    public List<T> GetUnitsByType<T>() where T : class, IHealth
    {
        List<T> result = new List<T>();
        foreach (var unit in allUnits)
        {
            if (unit is T typedUnit)
            {
                result.Add(typedUnit);
            }
        }
        return result;
    }
}