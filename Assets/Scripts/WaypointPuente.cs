using UnityEngine;

public class WaypointPuente : MonoBehaviour
{
    [Header("Conexión del Puente")]
    public WaypointPuente waypointConectado;

    [Header("Configuración Visual")]
    public Color colorGizmo = Color.cyan;
    public float radioGizmo = 0.5f;

    void OnDrawGizmos()
    {
        // Dibujar el waypoint
        Gizmos.color = colorGizmo;
        Gizmos.DrawWireSphere(transform.position, radioGizmo);

        // Dibujar conexión si existe
        if (waypointConectado != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, waypointConectado.transform.position);

            // Flecha direccional
            Vector3 direccion = (waypointConectado.transform.position - transform.position).normalized;
            Vector3 perpendicular = new Vector3(-direccion.y, direccion.x, 0) * 0.2f;
            Vector3 cabezaFlecha = waypointConectado.transform.position - direccion * 0.3f;

            Gizmos.DrawLine(cabezaFlecha, cabezaFlecha - direccion * 0.5f + perpendicular);
            Gizmos.DrawLine(cabezaFlecha, cabezaFlecha - direccion * 0.5f - perpendicular);
        }

        // Etiqueta con nombre
#if UNITY_EDITOR
        GUIStyle estilo = new GUIStyle();
        estilo.normal.textColor = colorGizmo;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, name, estilo);
#endif
    }

    public bool EstaConectado()
    {
        return waypointConectado != null;
    }
}