using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyBase : MonoBehaviour
{
    [Header("Configuración de Conquista")]
    public float conquestTime = 30f;
    public float conquestRange = 5f;

    [Header("Velocidades de Conquista")]
    public float growthSpeed = 1f;
    public float decaySpeed = 0.5f;

    [Header("UI Elements")]
    public Slider conquestSlider;
    public Vector3 sliderPosition = new Vector3(-1309.61f, -144.46f, 0f);
    public GameObject victoryCanvas;

    [Header("Estados")]
    public bool isConquered = false;

    // Variables internas
    private float conquestProgress = 0f;
    private Canvas sliderCanvas;
    private List<PlayerBuildingDetector> conqueringPlayers = new List<PlayerBuildingDetector>();

    void Start()
    {
        InitializeBase();
        SetupSlider();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);
    }

    void InitializeBase()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = conquestRange;

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }
    }

    void SetupSlider()
    {
        if (conquestSlider != null)
        {
            GameObject canvasGO = new GameObject("EnemyBaseCanvas");
            canvasGO.transform.SetParent(transform);
            sliderCanvas = canvasGO.AddComponent<Canvas>();
            sliderCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = sliderCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);

            sliderCanvas.transform.position = sliderPosition;
            sliderCanvas.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            conquestSlider.transform.SetParent(sliderCanvas.transform);
            conquestSlider.minValue = 0f;
            conquestSlider.maxValue = 1f;
            conquestSlider.value = 0f;
            conquestSlider.gameObject.SetActive(false);

            conquestSlider.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        // Si el juego está pausado, no actualizar nada
        if (Time.timeScale == 0) return;

        if (!isConquered)
        {
            UpdateConquestProgress();
        }
    }

    void UpdateConquestProgress()
    {
        if (conqueringPlayers.Count > 0)
        {
            float progressIncrement = (growthSpeed * conqueringPlayers.Count) / conquestTime;
            conquestProgress += progressIncrement * Time.deltaTime;
            conquestProgress = Mathf.Min(conquestProgress, conquestTime);

            if (conquestProgress > 0 && conquestSlider != null && !conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(true);
            }
        }
        else if (conquestProgress > 0)
        {
            float progressDecrement = decaySpeed / conquestTime;
            conquestProgress -= progressDecrement * Time.deltaTime;
            conquestProgress = Mathf.Max(conquestProgress, 0);

            if (conquestProgress <= 0 && conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
            {
                conquestSlider.gameObject.SetActive(false);
            }
        }

        if (conquestSlider != null && conquestSlider.gameObject.activeInHierarchy)
        {
            conquestSlider.value = conquestProgress / conquestTime;
        }

        if (conquestProgress >= conquestTime && !isConquered)
        {
            CompleteConquest();
        }
    }

    public void RegisterPlayer(PlayerBuildingDetector player)
    {
        if (!conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Add(player);
        }
    }

    public void UnregisterPlayer(PlayerBuildingDetector player)
    {
        if (conqueringPlayers.Contains(player))
        {
            conqueringPlayers.Remove(player);
        }
    }

    private void CompleteConquest()
    {
        isConquered = true;

        if (conquestSlider != null)
        {
            conquestSlider.gameObject.SetActive(false);
        }

        conqueringPlayers.Clear();
        ActivateVictoryCanvas();

        Debug.Log("¡BASE ENEMIGA CONQUISTADA!");
    }

    private void ActivateVictoryCanvas()
    {
        // PAUSAR EL JUEGO COMPLETAMENTE
        Time.timeScale = 0f;

        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(true);
            Debug.Log("Canvas de victoria activado - JUEGO PAUSADO");
        }
        else
        {
            Debug.LogWarning("Victory Canvas no asignado en EnemyBase");
        }
    }

    // Método para reanudar el juego (si necesitas botón de continuar)
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isConquered ? Color.blue : (conqueringPlayers.Count > 0 ? Color.yellow : Color.red);
        Gizmos.DrawWireSphere(transform.position, conquestRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sliderPosition, 0.5f);
    }

    void OnDestroy()
    {
        if (sliderCanvas != null)
        {
            Destroy(sliderCanvas.gameObject);
        }
    }
}