using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EdificioInfo2D : MonoBehaviour
{
    [Header("Configuración de Escena")]
    public string nombreEscenaDestino;

    [Header("Información del Edificio")]
    public string nombreEdificio = "Edificio";
    [TextArea(2, 3)]
    public string descripcionEdificio = "Haz click para entrar";

    [Header("Tooltip 2D")]
    public GameObject tooltipPrefab;
    public Vector2 offset = new Vector2(0, -1f); // Debajo del edificio
    public float escalaTooltip = 1f;

    private GameObject tooltipInstance;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Crear tooltip si existe el prefab
        if (tooltipPrefab != null)
        {
            CrearTooltip2D();
        }
    }

    void CrearTooltip2D()
    {
        tooltipInstance = Instantiate(tooltipPrefab, transform);
        tooltipInstance.transform.localPosition = offset;
        tooltipInstance.transform.localScale = Vector3.one * escalaTooltip;
        tooltipInstance.SetActive(false);

        // Configurar orden de renderizado
        ConfigurarSortingOrder();
    }

    void ConfigurarSortingOrder()
    {
        // Asegurar que el tooltip se renderice por encima del edificio
        Canvas canvas = tooltipInstance.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 100;
        }

        // Si usa SpriteRenderer en lugar de Canvas
        SpriteRenderer tooltipSprite = tooltipInstance.GetComponent<SpriteRenderer>();
        if (tooltipSprite != null)
        {
            tooltipSprite.sortingLayerName = "UI";
            tooltipSprite.sortingOrder = 100;
        }
    }

    void OnMouseEnter()
    {
        MostrarTooltip();
    }

    void OnMouseExit()
    {
        OcultarTooltip();
    }

    void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }

    void MostrarTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(true);
            ActualizarTexto();
        }
        else
        {
            // Fallback: mostrar en consola
            Debug.Log($"{nombreEdificio}: {descripcionEdificio}");
        }
    }

    void OcultarTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
    }

    void ActualizarTexto()
    {
        // Buscar componentes de texto y actualizar
        TextMeshProUGUI[] textos = tooltipInstance.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI texto in textos)
        {
            if (texto.name == "TextoNombre" || texto.name.Contains("Nombre"))
                texto.text = nombreEdificio;
            else if (texto.name == "TextoDescripcion" || texto.name.Contains("Desc"))
                texto.text = descripcionEdificio;
        }

        // Soporte para Text legacy (opcional)
        Text[] textosLegacy = tooltipInstance.GetComponentsInChildren<Text>();
        foreach (Text texto in textosLegacy)
        {
            if (texto.name == "TextoNombre" || texto.name.Contains("Nombre"))
                texto.text = nombreEdificio;
            else if (texto.name == "TextoDescripcion" || texto.name.Contains("Desc"))
                texto.text = descripcionEdificio;
        }
    }
}