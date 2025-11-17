using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<PlayerHealth> allPlayers = new List<PlayerHealth>();
    private bool gameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(PlayerHealth player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
        }
    }

    public void UnregisterPlayer(PlayerHealth player)
    {
        if (allPlayers.Contains(player))
        {
            allPlayers.Remove(player);

            // Verificar si todos los jugadores han muerto
            CheckGameOver();
        }
    }

    public void AddNewPlayer(PlayerHealth newPlayer)
    {
        RegisterPlayer(newPlayer);
    }

    private void CheckGameOver()
    {
        if (allPlayers.Count == 0 && !gameOver)
        {
            gameOver = true;
            Invoke("LoadGameOverScene", 1f);
        }
    }

    public int GetActivePlayersCount()
    {
        return allPlayers.Count;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    private void LoadGameOverScene()
    {
        SceneManager.LoadScene("Game Over");
    }

    public void ResetGame()
    {
        allPlayers.Clear();
        gameOver = false;
    }
}