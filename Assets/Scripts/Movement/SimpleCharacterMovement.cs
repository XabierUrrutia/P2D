using System.Collections.Generic;
using UnityEngine;

public class SimpleCharacterMovement : MonoBehaviour
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

    [Header("Sprites - 8 direcciones (2 frames + idle)")]
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

    void Start()
    {
        cam = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        objetivo = transform.position;

        ActualizarSprite(ultimaDireccion, false, true);
    }

    void Update()
    {
        ProcesarInput();
        Mover();
        ActualizarCooldown();
        ActualizarAnimacion();
    }

    void ProcesarInput()
    {
        if (Input.GetMouseButtonDown(1) && puedeClickar)
        {
            Vector3 posicionRaton = cam.ScreenToWorldPoint(Input.mousePosition);
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

    void CalcularRutaInteligente(Vector3 inicio, Vector3 destino)
    {
        puntosCamino.Clear();
        moviendose = false;

        // PRIMERO: Verificar si el camino directo es posible (sin agua)
        if (!CaminoTieneAgua(inicio, destino))
        {
            // Camino directo sin agua
            puntosCamino.Add(destino);
            Debug.Log("Usando camino directo (sin agua)");
            moviendose = true;
        }
        else
        {
            Debug.Log("Camino tiene agua - buscando puente necesario...");

            // LÓGICA SIMPLIFICADA: Buscar solo el puente más cercano que realmente conecte
            WaypointPuente puenteUtil = EncontrarPuenteSimple(inicio, destino);

            if (puenteUtil != null && puenteUtil.waypointConectado != null)
            {
                Debug.Log($"Usando puente: {puenteUtil.name}");

                // Solo usar este puente si realmente conecta con el destino
                if (!CaminoTieneAgua(puenteUtil.waypointConectado.transform.position, destino))
                {
                    puntosCamino.Add(puenteUtil.transform.position);
                    puntosCamino.Add(puenteUtil.waypointConectado.transform.position);
                    puntosCamino.Add(destino);
                    moviendose = true;
                }
                else
                {
                    Debug.LogWarning("El puente no conecta con el destino - NO MOVER");
                }
            }
            else
            {
                Debug.LogWarning("No se encontró un puente usable. NO SE PUEDE LLEGAR AL DESTINO.");
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
                // VERIFICACIÓN SIMPLE: 
                // 1. El camino al puente NO debe tener agua
                // 2. El camino desde el puente conectado al destino NO debe tener agua
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

        // Mover hacia el objetivo
        transform.position += direccion * velocidad * Time.deltaTime;
        direccionMovimiento = direccion;

        // Verificar si llegamos al punto actual
        if (Vector3.Distance(transform.position, objetivoActual) < distanciaParada)
        {
            puntosCamino.RemoveAt(0);

            if (puntosCamino.Count == 0)
            {
                moviendose = false;
                Debug.Log("Destino alcanzado");
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

        // Distribución de direcciones
        if (angulo >= 337.5f || angulo < 22.5f)        // Derecha
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 22.5f && angulo < 67.5f)    // Arriba-Derecha
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 67.5f && angulo < 112.5f)   // Arriba
            spriteSeleccionado = idle ? atrasDerecha_Idle : (alternar ? atrasDerecha_L : atrasDerecha_R);
        else if (angulo >= 112.5f && angulo < 157.5f)  // Arriba-Izquierda
            spriteSeleccionado = idle ? atrasIzquierda_Idle : (alternar ? atrasIzquierda_L : atrasIzquierda_R);
        else if (angulo >= 157.5f && angulo < 202.5f)  // Izquierda
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 202.5f && angulo < 247.5f)  // Abajo-Izquierda
            spriteSeleccionado = idle ? frenteIzquierda_Idle : (alternar ? frenteIzquierda_L : frenteIzquierda_R);
        else if (angulo >= 247.5f && angulo < 292.5f)  // Abajo
            spriteSeleccionado = idle ? frenteDerecha_Idle : (alternar ? frenteDerecha_L : frenteDerecha_R);
        else if (angulo >= 292.5f && angulo < 337.5f)  // Abajo-Derecha
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

    // Para debug visual
    void OnDrawGizmosSelected()
    {
        // Dibujar camino planeado
        if (puntosCamino != null && puntosCamino.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 anterior = transform.position;

            foreach (Vector3 punto in puntosCamino)
            {
                Gizmos.DrawWireSphere(punto, 0.2f);
                Gizmos.DrawLine(anterior, punto);
                anterior = punto;
            }
        }

        // Dibujar dirección actual
        if (moviendose)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, direccionMovimiento * 1f);
        }

        // Dibujar área de búsqueda de waypoints
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 12f);
    }
}