using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Painel do menu de pausa
    public bool isPaused = false;

    void Start()
    {
        // Garante que o jogo começa sem estar em pausa
        ResumeGame();
    }

    void Update()
    {
        // Também podes permitir pausar com a tecla Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;  // pausa TODA a física e Update()
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;  // retoma o jogo
        isPaused = false;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // volta ao normal antes de trocar de cena
        SceneManager.LoadScene(0); // exemplo: menu principal
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}
