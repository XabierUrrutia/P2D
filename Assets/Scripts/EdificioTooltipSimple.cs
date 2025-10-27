using UnityEngine;
using UnityEngine.SceneManagement;

public class EdificioTooltipSimple : MonoBehaviour
{
    [Header("Configuración")]
    public string nombreEscenaDestino;
    public string nombreEdificio = "Edificio";
    public string descripcion = "Haz click para entrar";

    [Header("Tooltip Simple")]
    public bool usarTooltipSimple = true;
    public float alturaTooltip = 2f;

    private GameObject tooltipSimple;
    private TextMesh textMesh;

    void Start()
    {
        if (usarTooltipSimple)
        {
            CrearTooltipSimple();
        }
    }

    void CrearTooltipSimple()
    {
        // Crear GameObject para el texto
        tooltipSimple = new GameObject("Tooltip_" + nombreEdificio);
        tooltipSimple.transform.SetParent(transform);
        tooltipSimple.transform.localPosition = new Vector3(0, alturaTooltip, 0);

        // Añadir TextMesh
        textMesh = tooltipSimple.AddComponent<TextMesh>();
        textMesh.text = nombreEdificio + "\n" + descripcion;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 20;
        textMesh.color = Color.white;

        // Añadir fondo (opcional)
        var background = tooltipSimple.AddComponent<SpriteRenderer>();
        background.sprite = CreateSimpleSprite();
        background.color = new Color(0, 0, 0, 0.8f);
        background.sortingOrder = 99;

        // Ajustar tamaño del fondo al texto
        Bounds textBounds = textMesh.GetComponent<Renderer>().bounds;
        background.transform.localScale = new Vector3(textBounds.size.x + 0.2f, textBounds.size.y + 0.1f, 1f);

        tooltipSimple.SetActive(false);
    }

    Sprite CreateSimpleSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
    }

    private void OnMouseEnter()
    {
        if (tooltipSimple != null)
        {
            tooltipSimple.SetActive(true);
        }

        // Hacer que mire siempre a la cámara
        if (tooltipSimple != null)
        {
            tooltipSimple.transform.LookAt(Camera.main.transform);
            tooltipSimple.transform.Rotate(0, 180, 0);
        }
    }

    private void OnMouseExit()
    {
        if (tooltipSimple != null)
        {
            tooltipSimple.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }

    void Update()
    {
        // Mantener el tooltip mirando a la cámara
        if (tooltipSimple != null && tooltipSimple.activeInHierarchy)
        {
            tooltipSimple.transform.LookAt(Camera.main.transform);
            tooltipSimple.transform.Rotate(0, 180, 0);
        }
    }
}