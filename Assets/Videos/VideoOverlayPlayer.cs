using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
using System;

public class VideoOverlayPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Defaults")]
    public VideoClip defaultClip;
    public bool allowSkip = true;

    [Header("Disable while playing (optional)")]
    public GameObject[] disableWhilePlaying;

    private Action onFinish;
    private bool isPlaying;

    void Awake()
    {
        Hide();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (!isPlaying || !allowSkip) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            Finish();
    }

    public void PlayDefaultAndLoad(string sceneName)
    {
        if (!defaultClip) { Debug.LogWarning("[VideoOverlayPlayer] Default clip em falta."); return; }
        Play(defaultClip, () => SceneManager.LoadScene(sceneName));
    }

    public void Play(VideoClip clip, Action after)
    {
        if (!clip) { Debug.LogWarning("[VideoOverlayPlayer] Clip em falta."); return; }

        onFinish = after;
        Show();

        foreach (var go in disableWhilePlaying)
            if (go) go.SetActive(false);

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;

        videoPlayer.loopPointReached -= OnVideoEnded;
        videoPlayer.loopPointReached += OnVideoEnded;

        isPlaying = true;
        videoPlayer.Play();
    }

    private void OnVideoEnded(VideoPlayer vp) => Finish();

    private void Finish()
    {
        if (!isPlaying) return;
        isPlaying = false;

        videoPlayer.loopPointReached -= OnVideoEnded;
        videoPlayer.Stop();

        Hide();

        foreach (var go in disableWhilePlaying)
            if (go) go.SetActive(true);

        onFinish?.Invoke();
        onFinish = null;
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
