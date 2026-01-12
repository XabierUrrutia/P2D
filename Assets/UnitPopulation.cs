using UnityEngine;

public class UnitPopulation : MonoBehaviour
{
    [Header("Configuración de Población")]
    public PopulationManager.TipoUnidad soyUnaUnidadDe;
    public int costePoblacion = 1;

    // START se ejecuta cuando el soldado APARECE en el juego
    void Start()
    {
        if (PopulationManager.Instance != null)
        {
            // ¡IMPORTANTE!: Aquí llamamos a REGISTRAR (Sumar)
            PopulationManager.Instance.RegistrarUnidad(soyUnaUnidadDe, costePoblacion);
        }
    }

    // ONDESTROY se ejecuta cuando el soldado se DESTRUYE/MUERE
    void OnDestroy()
    {
        // --- CORRECCIÓN CLAVE ---
        // Si el script estaba desactivado (era un fantasma/preview), 
        // significa que nunca ejecutó el Start(), así que NO debemos restar.
        if (!this.enabled) return;
        // ------------------------

        if (PopulationManager.Instance != null && gameObject.scene.isLoaded)
        {
            PopulationManager.Instance.EliminarUnidad(soyUnaUnidadDe, costePoblacion);
        }
    }
}