using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SelectableUnit))] // Obliga a tener el componente visual
public class SimpleCharacterMovement : MonoBehaviour, ISelectableUnit
{
    [Header("Movimiento Básico")]
    public float velocidad = 4f;
    public float distanciaParada = 0.1f;

    [Header("Cooldown")]
    public float cooldownClick = 1.5f;
    private float ultimoClickTime;
    private bool puedeClickar = true;

    [Header("Detección de Terreno")]
    public LayerMask capaSuelo;
    public LayerMask capaAgua;
    public LayerMask capaWaypointPuente;

    [Header("Sprites - 8 direcciones")]
    public Sprite frenteDerecha_L;
    public Sprite frenteDerecha_R;
    public Sprite frenteDerecha_Idle;
    public Sprite frenteIzquierda_L;
    public Sprite frenteIzquierda_R;
    public Sprite frenteIzquierda_Idle;
    public Sprite atrasDerecha_L;
    public Sprite atrasDerecha_R;
    public Sprite atrasDerecha_Idle;
    public Sprite atrasIzquierda_L;
    public Sprite atrasIzquierda_R;
    public Sprite atrasIzquierda_Idle;

    [Header("Marcador de Click")]
    public GameObject prefabMarcadorClick;

    // YA NO USAMOS ESTO MANUALMENTE (Lo gestiona SelectableUnit)
    // public GameObject indicadorSeleccion; 

    // Referencia al componente unificador
    private SelectableUnit selectableUnitComponent;

    // Variables internas
    private Vector3 objetivo;
    private bool moviendose = false;
    private List<Vector3> puntosCamino = new List<Vector3>();
    private Camera cam;
    private Vector2 direccionMovimiento;
    private SpriteRenderer spriteRenderer;

    // Animación
    private float temporizadorAnim = 0f;
    private bool alternarAnim = false;
    private Vector2 ultimaDireccion = new Vector2(1, -1);

    // Estado Selección
    private bool estaSeleccionado = false;

    void Start()
    {
        cam = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        objetivo = transform.position;

        // Obtenemos el componente que controla la flecha y el HUD
        selectableUnitComponent = GetComponent<SelectableUnit>();

        ActualizarSprite(ultimaDireccion, false, true);
    }

    void Update()
    {
        Mover();
        ActualizarCooldown();
        ActualizarAnimacion();
    }

    // --- INTERFAZ ISelectableUnit (La clave para que funcione el Manager) ---

    public void Seleccionar()
    {
        estaSeleccionado = true;
        // Delegamos lo visual al componente SelectableUnit
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(true);

        Debug.Log($"{name} seleccionado");
    }

    public void Deseleccionar()
    {
        estaSeleccionado = false;
        // Delegamos lo visual al componente SelectableUnit
        if (selectableUnitComponent != null)
            selectableUnitComponent.ShowSelection(false);

        Debug.Log($"{name} deseleccionado");
    }

    // --- MOVIMIENTO ---

    public void MoverADestino(Vector3 destino)
    {
        if (puedeClickar && estaSeleccionado)
        {
            Vector3 posicionRaton = destino;
            posicionRaton.z = 0;

            if (EsSueloValido(posicionRaton))
            {
                ConfigurarCooldown();
                CalcularRutaInteligente(transform.position, posicionRaton);

                if (prefabMarcadorClick != null)
                {
                    GameObject marcador = Instantiate(prefabMarcadorClick, posicionRaton, Quaternion.identity);
                    Destroy(marcador, 1f);
                }
            }
        }
    }

    // (El resto de tu lógica de movimiento se mantiene igual)
    void CalcularRutaInteligente(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;

        if (!CaminoTieneAgua(inicio, destino))
        {
            puntosCamino.Add(destino);
            moviendose = true;
        }
        else
        {
            WaypointPuente puenteUtil = EncontrarPuenteSimple(inicio, destino);

            if (puenteUtil != null && puenteUtil.waypointConectado != null)
            {
                if (!CaminoTieneAgua(puenteUtil.waypointConectado.transform.position, destino))
                {
                    puntosCamino.Add(puenteUtil.transform.position);
                    puntosCamino.Add(puenteUtil.waypointConectado.transform.position);
                    puntosCamino.Add(destino);
                    moviendose = true;
                }
            }
        }
    }

    WaypointPuente EncontrarPuenteSimple(Vector3 inicio, Vector3 destino)
    {
        Collider2D[] todosWaypoints = Physics2D.OverlapCircleAll(inicio, 12f, capaWaypointPuente);
        WaypointPuente mejorPuente = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Collider2D collider in todosWaypoints)
        {
            WaypointPuente waypoint = collider.GetComponent<WaypointPuente>();
            if (waypoint != null && waypoint.waypointConectado != null)
            {
                bool caminoAlPuenteSeguro = !CaminoTieneAgua(inicio, waypoint.transform.position);
                bool caminoDelPuenteSeguro = !CaminoTieneAgua(waypoint.waypointConectado.transform.position, destino);

                if (caminoAlPuenteSeguro && caminoDelPuenteSeguro)
                {
                    float distancia = Vector3.Distance(inicio, waypoint.transform.position);
                    if (distancia < menorDistancia)
                    {
                        menorDistancia = distancia;
                        mejorPuente = waypoint;
                    }
                }
            }
        }
        return mejorPuente;
    }

    void Mover()
    {
        if (!moviendose || puntosCamino.Count == 0) return;

        Vector3 objetivoActual = puntosCamino[0];
        Vector3 direccion = (objetivoActual - transform.position).normalized;
        transform.position += direccion * velocidad * Time.deltaTime;
        direccionMovimiento = direccion;

        if (Vector3.Distance(transform.position, objetivoActual) < distanciaParada)
        {
            puntosCamino.RemoveAt(0);
            if (puntosCamino.Count == 0)
            {
                moviendose = false;
            }
        }
    }

    void ActualizarAnimacion()
    {
        if (moviendose && direccionMovimiento.magnitude > 0.1f)
        {
            temporizadorAnim += Time.deltaTime;
            if (temporizadorAnim >= 0.2f)
            {
                temporizadorAnim = 0f;
                alternarAnim = !alternarAnim;
            }
            ActualizarSprite(direccionMovimiento, alternarAnim);
            ultimaDireccion = direccionMovimiento;
        }
        else
        {
            ActualizarSprite(ultimaDireccion, false, true);
        }
    }

    void ActualizarSprite(Vector2 direccion, bool alternar, bool idle = false)
    {
        if (spriteRenderer == null) return;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        if (angulo < 0) angulo += 360;

        Sprite spriteSeleccionado = frenteDerecha_Idle;

        if (angulo >= 337.5f || angulo < 22.5f)
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 22.5f && angulo < 67.5f)
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 67.5f && angulo < 112.5f)
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 112.5f && angulo < 157.5f)
            spriteSeleccionado = idle ? atrasIzquierda_Idle : (alternar ? atrasIzquierda_L : atrasIzquierda_R);
        else if (angulo >= 157.5f && angulo < 202.5f)
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 202.5f && angulo < 247.5f)
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 247.5f && angulo < 292.5f)
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 292.5f && angulo < 337.5f)
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);

        spriteRenderer.sprite = spriteSeleccionado;
    }

    bool CaminoTieneAgua(Vector3 inicio, Vector3 fin)
    {
        float distancia = Vector3.Distance(inicio, fin);
        if (distancia < 0.1f) return false;

        int muestras = Mathf.CeilToInt(distancia / 0.3f);
        for (int i = 0; i <= muestras; i++)
        {
            float t = (float)i / (float)muestras;
            Vector3 punto = Vector3.Lerp(inicio, fin, t);
            if (Physics2D.OverlapCircle(punto, 0.2f, capaAgua) &&
                !Physics2D.OverlapCircle(punto, 0.2f, capaWaypointPuente))
            {
                return true;
            }
        }
        return false;
    }

    bool EsSueloValido(Vector3 posicion)
    {
        return Physics2D.OverlapCircle(posicion, 0.3f, capaSuelo) != null;
    }

    void ConfigurarCooldown()
    {
        puedeClickar = false;
        ultimoClickTime = Time.time;
    }

    void ActualizarCooldown()
    {
        if (!puedeClickar && Time.time - ultimoClickTime >= cooldownClick)
        {
            puedeClickar = true;
        }
    }
}