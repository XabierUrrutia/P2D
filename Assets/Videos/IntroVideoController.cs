using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] VideoPlayer vp;
    [SerializeField] string menuSceneName = "Menu";
    [SerializeField] bool allowSkip = true;

    void Awake()
    {
        vp.loopPointReached += _ => LoadMenu();
    }

    void Update()
    {
        if (!allowSkip) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            LoadMenu();
    }

    void LoadMenu()
    {
        vp.loopPointReached -= _ => LoadMenu();
        SceneManager.LoadScene(menuSceneName);
    }
}
