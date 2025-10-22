using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private static Stack<int> sceneHistory = new Stack<int>(); // Pilha para armazenar as cenas visitadas

    void OnEnable()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Evita adicionar a mesma cena consecutivamente no hist�rico
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentSceneIndex)
        {
            sceneHistory.Push(currentSceneIndex);
        }
    }

    private void LoadSceneAndSave(int sceneIndex)
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Salva a cena atual antes de mudar
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentSceneIndex)
        {
            sceneHistory.Push(currentSceneIndex);
        }

        SceneManager.LoadScene(sceneIndex);
    }

    public void GoToSettingsMenu()
    {
        LoadSceneAndSave(1);
    }
    public void GoToOptionsFromGame()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerPositionManager.SavePosition(player.transform.position);
        }

        SceneManager.LoadScene(3);
    }

    public void GoToMainMenu()
    {
        LoadSceneAndSave(0);
    }

    public void GotoGame()
    {
        LoadSceneAndSave(2);
    }

    public void GoInGameSettings()
    {
        LoadSceneAndSave(2);
    }

    public void GoPlayerChooseGame()
    {
        LoadSceneAndSave(8);
    }

    public void GoProgress()
    {
        LoadSceneAndSave(4);
    }
    public void GoToAccount()
    {
        LoadSceneAndSave(5);
    }

    public void GoToMainSettings()
    {
        LoadSceneAndSave(6);
    }

    public void GoToToKenConf()
    {
        LoadSceneAndSave(7);
    }

    public void GoBack()
    {
        if (sceneHistory.Count > 1) // Mant�m sempre pelo menos uma cena na pilha
        {
            sceneHistory.Pop(); // Remove a cena atual
            int previousSceneIndex = sceneHistory.Peek(); // Obt�m a cena anterior

            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            Debug.Log("Nenhuma cena anterior no hist�rico!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }
}
