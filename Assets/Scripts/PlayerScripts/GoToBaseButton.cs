using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mostra um botão no HUD quando a PlayerBase está fora da vista da câmera.
/// Ao clicar, faz uma animação suave da câmera até à posição da base.
/// </summary>
[DisallowMultipleComponent]
public class GoToBaseButton : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Botão de HUD que vai aparecer quando a base estiver fora da câmera.")]
    public Button goToBaseButton;

    [Tooltip("Transform da PlayerBase. Se vazio, será encontrado por PlayerBase na cena.")]
    public Transform playerBaseTransform;

    [Tooltip("Câmera de jogo. Se vazio, usa Camera.main.")]
    public Camera mainCamera;

    [Tooltip("Script de follow da câmera (por ex. cameraFollow). Se definido, será temporariamente desativado durante a animação.")]
    public MonoBehaviour cameraFollowScript;

    [Header("Detecção de visibilidade")]
    [Tooltip("Margem de tolerância dentro da viewport (0 = limite exacto, 0.1 = só considera fora se estiver bem fora do ecrã).")]
    [Range(-0.5f, 0.5f)]
    public float viewportMargin = 0.05f;

    [Header("Animação")]
    [Tooltip("Duração em segundos da animação de movimento da câmera até à base.")]
    public float moveDuration = 0.7f;

    [Tooltip("Curva de easing para a animação.")]
    public AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isMoving;
    private Coroutine moveCoroutine;

    void Awake()
    {
        if (goToBaseButton != null)
        {
            goToBaseButton.onClick.RemoveAllListeners();
            goToBaseButton.onClick.AddListener(OnGoToBaseClicked);
            goToBaseButton.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerBaseTransform == null)
        {
            var pb = FindObjectOfType<PlayerBase>();
            if (pb != null)
                playerBaseTransform = pb.transform;
        }

        if (playerBaseTransform == null)
        {
            Debug.LogWarning("[GoToBaseButton] PlayerBase não encontrada na cena.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (mainCamera == null || playerBaseTransform == null || goToBaseButton == null)
            return;

        // Não mostrar/ocultar botão enquanto animação está a correr
        if (isMoving)
            return;

        bool baseOffScreen = IsBaseOffScreen();

        if (goToBaseButton.gameObject.activeSelf != baseOffScreen)
        {
            goToBaseButton.gameObject.SetActive(baseOffScreen);
        }
    }

    bool IsBaseOffScreen()
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(playerBaseTransform.position);

        // Se z < 0, está atrás da câmera (fora)
        if (viewportPos.z < 0f)
            return true;

        // Considera "fora" se x/y estiverem fora de [0- margin, 1 + margin]
        float min = 0f - viewportMargin;
        float max = 1f + viewportMargin;

        bool inside =
            viewportPos.x >= min && viewportPos.x <= max &&
            viewportPos.y >= min && viewportPos.y <= max;

        return !inside;
    }

    void OnGoToBaseClicked()
    {
        if (mainCamera == null || playerBaseTransform == null || isMoving)
            return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveCameraToBase());
    }

    IEnumerator MoveCameraToBase()
    {
        isMoving = true;

        // Desativa follow, se houver
        bool restoredFollow = false;
        if (cameraFollowScript != null && cameraFollowScript.enabled)
        {
            cameraFollowScript.enabled = false;
            restoredFollow = true;
        }

        Vector3 startPos = mainCamera.transform.position;
        Vector3 targetPos = playerBaseTransform.position;

        // Mantém a mesma altura Z da câmera, só move X/Y
        targetPos.z = startPos.z;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, moveDuration);
            float easedT = moveEase.Evaluate(Mathf.Clamp01(t));

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        mainCamera.transform.position = targetPos;

        // Opcional: após chegar à base, podes reativar o follow ou deixar a câmara parada
        if (cameraFollowScript != null && restoredFollow)
        {
            cameraFollowScript.enabled = true;
        }

        isMoving = false;

        // Como agora a base está no ecrã, esconde o botão
        if (goToBaseButton != null)
            goToBaseButton.gameObject.SetActive(false);
    }
}