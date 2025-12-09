using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Interface_Buttons : MonoBehaviour
{
    private static Stack<int> sceneHistory = new Stack<int>(); // Pilha para armazenar as cenas visitadas

    // Controle de Options em modo aditivo
    private static bool optionsOpen = false;
    private static int optionsSceneIndex = 1; // ajusta se o índice da cena Options for outro
    private static int gameSceneBeforeOptions = -1;

    void OnEnable()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Evita adicionar a mesma cena consecutivamente no histórico
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentSceneIndex)
        {
            sceneHistory.Push(currentSceneIndex);
        }
    }

    private void LoadSceneAndSave(int sceneIndex)
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Salva a cena atual antes de mudar (no histórico)
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != currentSceneIndex)
        {
            sceneHistory.Push(currentSceneIndex);
        }

        // Salva posição do jogador atual (se existir) para permitir "retomar" quando voltar
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerPositionManager.SavePosition(player.transform.position);
        }

        SceneManager.LoadScene(sceneIndex);
    }

    public void GoToSettingsMenu()
    {
        LoadSceneAndSave(1);
    }

    // Abre as Options em modo aditivo para preservar a cena de jogo
    public void GoToOptionsFromGame()
    {
        // salva posição do jogador (já fazes isto no PauseMenu, mas deixo aqui por segurança)
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerPositionManager.SavePosition(player.transform.position);
        }

        // se já estiver aberta, não faz nada
        if (optionsOpen) return;

        gameSceneBeforeOptions = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadOptionsAdditive());
    }

    IEnumerator LoadOptionsAdditive()
    {
        var op = SceneManager.LoadSceneAsync(optionsSceneIndex, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;

        optionsOpen = true;
        Debug.Log("Interface_Buttons: Options aberta em modo aditivo, jogo preservado.");
    }

    public void GoToMainMenu()
    {
        LoadSceneAndSave(0);
    }

    public void GotoGame()
    {
        LoadSceneAndSave(2);
    }

    public void GoToMap()
    {
        LoadSceneAndSave(2);
    }
    public void SecondLevel()
    {
        LoadSceneAndSave(3);
    }
    public void GoInGameSettings()
    {
        LoadSceneAndSave(2);
    }
    public void GoTutorial1()
    {
        LoadSceneAndSave(6);
    }
    public void GoToLevel1()
    {
        LoadSceneAndSave(4);
    }
    public void GoToLevel2()
    {
        LoadSceneAndSave(3);
    }
    public void GoToLevel3()
    {
        LoadSceneAndSave(5);
    }
    public void GoBack()
    {
        // Se as Options foram abertas em modo aditivo, fecha-as em vez de recarregar a cena anterior
        if (optionsOpen)
        {
            StartCoroutine(UnloadOptionsAdditive());
            return;
        }

        if (sceneHistory.Count > 1) // Mantém sempre pelo menos uma cena na pilha
        {
            sceneHistory.Pop(); // Remove a cena atual
            int previousSceneIndex = sceneHistory.Peek(); // Obtém a cena anterior

            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            Debug.Log("Nenhuma cena anterior no histórico!");
        }
    }

    IEnumerator UnloadOptionsAdditive()
    {
        // Opcional: reativa o tempo (se a Options marcou Time.timeScale=0)
        Time.timeScale = 1f;

        var op = SceneManager.UnloadSceneAsync(optionsSceneIndex);
        while (!op.isDone)
            yield return null;

        optionsOpen = false;
        Debug.Log("Interface_Buttons: Options descarregada (volta ao jogo na cena anterior).");

        // Restaura posição do jogador se houver (PlayerPositionManager guarda a posição)
        if (PlayerPositionManager.HasSavedPosition)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 savedPos = PlayerPositionManager.GetPosition();
                player.transform.position = savedPos;
                PlayerPositionManager.HasSavedPosition = false;
                Debug.Log($"Interface_Buttons: posição do jogador restaurada para {savedPos}");
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }
}