// GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<IHealth> allUnits = new List<IHealth>();
    private bool gameOver = false;

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

    // Método para registrar cualquier unidad que implemente IHealth
    public void RegisterUnit(IHealth unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            Debug.Log($"Unidad registrada: {unit.transform.name}. Total: {allUnits.Count}");
        }
    }

    // Método para desregistrar una unidad
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

    // Método para añadir nuevas unidades (alias para RegisterUnit)
    public void AddNewUnit(IHealth newUnit)
    {
        RegisterUnit(newUnit);
    }

    private void CheckGameOver()
    {
        // Filtrar unidades muertas o nulas
        allUnits.RemoveAll(unit => unit == null || unit.IsDead);

        if (allUnits.Count == 0 && !gameOver)
        {
            gameOver = true;
            Debug.Log("¡Todas las unidades han muerto! Fin del juego.");
            Invoke("LoadGameOverScene", 1f);
        }
    }

    // Obtener cantidad de unidades activas
    public int GetActiveUnitsCount()
    {
        // Filtrar solo unidades vivas
        int aliveCount = 0;
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
                aliveCount++;
        }
        return aliveCount;
    }

    // Verificar si el juego ha terminado
    public bool IsGameOver()
    {
        return gameOver;
    }

    // Cargar escena de Game Over
    private void LoadGameOverScene()
    {
        SoundColector.Instance?.PlayDefeatMusic();

        SceneManager.LoadScene(9);
    }

    // Resetear el juego
    public void ResetGame()
    {
        allUnits.Clear();
        gameOver = false;
    }

    // Método para obtener todas las unidades (útil para AI, etc.)
    public List<IHealth> GetAllUnits()
    {
        return new List<IHealth>(allUnits);
    }

    // Método para obtener unidades por tipo (opcional)
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