using UnityEngine;
using UnityEngine.SceneManagement;

public class EdificioClickeable : MonoBehaviour
{
    [Header("Configuración de Escena")]
    public string nombreEscenaDestino;

    [Header("Efectos Visuales")]
    public bool mostrarEfectoHover = true;
    public Color colorHover = Color.yellow;
    public string nombreEdificio = "Edificio";

    private Color colorOriginal;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        // Obtener el SpriteRenderer y guardar el color original
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    private void OnMouseDown()
    {
        CambiarEscena();
    }

    public void CambiarEscena()
    {
        if (!string.IsNullOrEmpty(nombreEscenaDestino))
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
        else
        {
            Debug.LogError("No se ha asignado una escena destino para este edificio");
        }
    }

    // Efecto al pasar el mouse: solo cambio de color
    private void OnMouseEnter()
    {
        if (mostrarEfectoHover && spriteRenderer != null)
        {
            spriteRenderer.color = colorHover;
        }
    }

    private void OnMouseExit()
    {
        if (mostrarEfectoHover && spriteRenderer != null)
        {
            spriteRenderer.color = colorOriginal;
        }
    }
}