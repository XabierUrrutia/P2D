using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EdificioInfo2D : MonoBehaviour
{
    [Header("Configuración del Edificio")]
    public string nombreEscenaDestino;
    public string nombreEdificio = "Edificio";
    [TextArea(2, 3)]
    public string descripcionEdificio = "Haz click para entrar";

    [Header("Tooltip Isométrico")]
    public GameObject tooltipPrefab;
    public Vector3 offset = new Vector3(0, -2f, 0); // Offset en el mundo isométrico
    public float escala = 0.015f;

    private GameObject miTooltip;
    private Camera camara;

    void Start()
    {
        camara = Camera.main;

        if (tooltipPrefab != null)
        {
            CrearTooltipIsometrico();
        }
    }

    void CrearTooltipIsometrico()
    {
        // Crear tooltip como hijo del canvas o del mundo, no del edificio
        miTooltip = Instantiate(tooltipPrefab);

        // Configurar canvas para mundo isométrico
        Canvas canvas = miTooltip.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            // Ajustar el tamaño del RectTransform para isométrico
            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 100);
        }

        ActualizarTextoTooltip();
        miTooltip.SetActive(false);
    }

    void Update()
    {
        // Actualizar posición del tooltip para que siga al edificio en isométrico
        if (miTooltip != null && miTooltip.activeInHierarchy)
        {
            ActualizarPosicionIsometrica();
        }
    }

    void ActualizarPosicionIsometrica()
    {
        if (miTooltip == null) return;

        // Posicionar el tooltip en el mundo isométrico
        Vector3 posicionMundo = transform.position + offset;
        miTooltip.transform.position = posicionMundo;

        // Mantener el tooltip mirando a la cámara isométrica
        if (camara != null)
        {
            miTooltip.transform.rotation = camara.transform.rotation;
        }

        // Ajustar escala
        miTooltip.transform.localScale = Vector3.one * escala;
    }

    void ActualizarTextoTooltip()
    {
        if (miTooltip == null) return;

        TextMeshProUGUI[] textosTMP = miTooltip.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI texto in textosTMP)
        {
            if (texto.name.Contains("Nombre") || texto.gameObject.name.Contains("Nombre"))
                texto.text = nombreEdificio;
            else if (texto.name.Contains("Desc") || texto.gameObject.name.Contains("Desc"))
                texto.text = descripcionEdificio;
        }
    }

    void OnMouseEnter()
    {
        if (miTooltip != null)
        {
            miTooltip.SetActive(true);
            ActualizarPosicionIsometrica();
        }
    }

    void OnMouseExit()
    {
        if (miTooltip != null)
        {
            miTooltip.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }

    void OnDestroy()
    {
        if (miTooltip != null)
        {
            Destroy(miTooltip);
        }
    }

    // Método para debug visual en el Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 posicionTooltip = transform.position + offset;
        Gizmos.DrawWireSphere(posicionTooltip, 0.3f);
        Gizmos.DrawLine(transform.position, posicionTooltip);
    }
}