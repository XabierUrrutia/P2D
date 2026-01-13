using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Necesario para funciones de lista (Any, etc)

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Configuración de Selección")]
    public LayerMask capaUnidades;
    public Sprite spriteCuadroSeleccion;

    [Header("Configuración de Formación")]
    public float radioFormacion = 1.5f;
    public bool usarFormacionCircular = true;

    [Header("Tweaks de Audio")]
    [Tooltip("Tiempo mínimo entre sonidos de movimiento.")]
    public float intervaloMinSomMovimiento = 0.15f;
    private float ultimoSomMovimientoTime = -999f;

    // Variables internas
    private Vector3 inicioArrastre;
    private Vector3 finArrastre;
    private bool arrastrando = false;
    private Camera cam;

    // Usamos la Interfaz para guardar tanto Tanques como Soldados
    private List<ISelectableUnit> unidadesSeleccionadas = new List<ISelectableUnit>();

    private SpriteRenderer spriteRenderer;
    private GameObject cuadroObj;

    void Start()
    {
        cam = Camera.main;

        // Crear cuadro visual
        cuadroObj = new GameObject("CuadroSeleccion");
        spriteRenderer = cuadroObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteCuadroSeleccion;
        spriteRenderer.color = new Color(0, 0.5f, 1f, 0.3f);
        spriteRenderer.sortingOrder = 100;
        cuadroObj.SetActive(false);
    }

    void Update()
    {
        ProcesarSeleccion();
        ProcesarMovimiento();
    }

    // ---------------------------------------------------------
    // CLICK IZQUIERDO (Seleccionar)
    // ---------------------------------------------------------
    void ProcesarSeleccion()
    {
        // 1. Click Abajo
        if (Input.GetMouseButtonDown(0))
        {
            inicioArrastre = Input.mousePosition;
            arrastrando = true;

            RaycastHit2D hit = Physics2D.Raycast(
                cam.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero,
                Mathf.Infinity,
                capaUnidades
            );

            if (hit.collider != null)
            {
                // Buscamos el componente INTERFAZ
                ISelectableUnit unidad = hit.collider.GetComponent<ISelectableUnit>();

                if (unidad != null)
                {
                    // Si no pulsamos Shift, limpiamos la selección anterior
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeseleccionarTodas();
                    }

                    bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                    SeleccionarUnidad(unidad);

                    if (eraNueva) PlaySelectionSoundFor(unidad);
                }
            }
            else
            {
                // Click en el vacío: Deseleccionar todo
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeseleccionarTodas();
                }
            }
        }

        // 2. Arrastrando
        if (arrastrando && Input.GetMouseButton(0))
        {
            finArrastre = Input.mousePosition;
            DibujarCuadroSeleccion();
        }

        // 3. Soltar Click
        if (Input.GetMouseButtonUp(0))
        {
            // Si hemos arrastrado suficiente distancia, es una selección de área
            if (arrastrando && Vector3.Distance(inicioArrastre, finArrastre) > 10f)
            {
                SeleccionarUnidadesEnCuadro();
            }
            arrastrando = false;
            OcultarCuadroSeleccion();
        }
    }

    // ---------------------------------------------------------
    // CLICK DERECHO (Mover)
    // ---------------------------------------------------------
    void ProcesarMovimiento()
    {
        // Limpiamos la lista por si alguna unidad murió
        unidadesSeleccionadas.RemoveAll(u => u == null || u.gameObject == null);

        if (Input.GetMouseButtonDown(1) && unidadesSeleccionadas.Count > 0)
        {
            Vector3 destino = cam.ScreenToWorldPoint(Input.mousePosition);
            destino.z = 0;

            // Calculamos formaciones
            Vector3[] destinos = CalcularDestinosDistribuidos(destino, unidadesSeleccionadas.Count);

            for (int i = 0; i < unidadesSeleccionadas.Count; i++)
            {
                if (unidadesSeleccionadas[i] != null)
                {
                    unidadesSeleccionadas[i].MoverADestino(destinos[i]);
                }
            }

            //Audio de movimiento
            if (SoundColector.Instance != null &&
                Time.time - ultimoSomMovimientoTime >= intervaloMinSomMovimiento)
                {
                    int tankCount = unidadesSeleccionadas.Count(u => u != null && u.gameObject != null && u.gameObject.CompareTag("Tank"));
                    int infantryCount = Mathf.Max(0, unidadesSeleccionadas.Count - tankCount);

                    SoundColector.Instance.PlayUnitMoveVoice(infantryCount, tankCount);
                    ultimoSomMovimientoTime = Time.time;
                }
        }
    }

    // ---------------------------------------------------------
    // GESTIÓN DE LA LISTA DE SELECCIÓN
    // ---------------------------------------------------------

    void SeleccionarUnidad(ISelectableUnit unidad)
    {
        if (!unidadesSeleccionadas.Contains(unidad))
        {
            unidadesSeleccionadas.Add(unidad);

            // AQUÍ ESTABA EL ERROR:
            // En vez de buscar "indicadorSeleccion.SetActive", llamamos al método .Seleccionar()
            // Y la unidad ya sabrá qué flecha encender internamente.
            unidad.Seleccionar();
        }
    }

    void DeseleccionarTodas()
    {
        foreach (ISelectableUnit unidad in unidadesSeleccionadas)
        {
            if (unidad != null && unidad.gameObject != null)
            {
                unidad.Deseleccionar();
            }
        }
        unidadesSeleccionadas.Clear();

        // Limpiar HUD si existe
        if (UnitHUDManager.Instance != null)
        {
            UnitHUDManager.Instance.SeleccionarUnidad(null);
        }
    }

    void SeleccionarUnidadesEnCuadro()
    {
        Vector2 min = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Min(inicioArrastre.x, finArrastre.x),
            Mathf.Min(inicioArrastre.y, finArrastre.y), 0));

        Vector2 max = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Max(inicioArrastre.x, finArrastre.x),
            Mathf.Max(inicioArrastre.y, finArrastre.y), 0));

        Collider2D[] unidadesEnArea = Physics2D.OverlapAreaAll(min, max, capaUnidades);

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeseleccionarTodas();
        }

        ISelectableUnit primeraNueva = null;

        foreach (Collider2D collider in unidadesEnArea)
        {
            ISelectableUnit unidad = collider.GetComponent<ISelectableUnit>();
            if (unidad != null)
            {
                bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                SeleccionarUnidad(unidad);

                if (eraNueva && primeraNueva == null) primeraNueva = unidad;
            }
        }

        if (primeraNueva != null) PlaySelectionSoundFor(primeraNueva);
    }

    // ---------------------------------------------------------
    // FORMACIONES
    // ---------------------------------------------------------
    Vector3[] CalcularDestinosDistribuidos(Vector3 destinoCentral, int cantidadUnidades)
    {
        Vector3[] destinos = new Vector3[cantidadUnidades];

        if (cantidadUnidades == 1)
        {
            destinos[0] = destinoCentral;
            return destinos;
        }

        if (usarFormacionCircular)
        {
            for (int i = 0; i < cantidadUnidades; i++)
            {
                float angulo = i * (2f * Mathf.PI / cantidadUnidades);
                float x = Mathf.Cos(angulo) * radioFormacion;
                float y = Mathf.Sin(angulo) * radioFormacion;
                destinos[i] = destinoCentral + new Vector3(x, y, 0);
            }
        }
        else
        {
            int filas = Mathf.CeilToInt(Mathf.Sqrt(cantidadUnidades));
            int columnas = Mathf.CeilToInt((float)cantidadUnidades / filas);

            int index = 0;
            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    if (index >= cantidadUnidades) break;

                    float x = (columna - (columnas - 1) * 0.5f) * radioFormacion;
                    float y = (fila - (filas - 1) * 0.5f) * radioFormacion;
                    destinos[index] = destinoCentral + new Vector3(x, y, 0);
                    index++;
                }
            }
        }

        return destinos;
    }

    // ---------------------------------------------------------
    // VISUALES Y SONIDO
    // ---------------------------------------------------------
    void DibujarCuadroSeleccion()
    {
        Vector3 inicioMundo = cam.ScreenToWorldPoint(new Vector3(inicioArrastre.x, inicioArrastre.y, 0));
        Vector3 finMundo = cam.ScreenToWorldPoint(new Vector3(finArrastre.x, finArrastre.y, 0));
        inicioMundo.z = 0; finMundo.z = 0;

        Vector3 centro = (inicioMundo + finMundo) / 2f;
        Vector3 tamaño = new Vector3(Mathf.Abs(finMundo.x - inicioMundo.x), Mathf.Abs(finMundo.y - inicioMundo.y), 1f);

        cuadroObj.transform.position = centro;
        cuadroObj.transform.localScale = tamaño;
        cuadroObj.SetActive(true);
    }

    void OcultarCuadroSeleccion()
    {
        cuadroObj.SetActive(false);
    }

    void PlaySelectionSoundFor(ISelectableUnit unidad)
    {
        if (SoundColector.Instance == null) return;

        int tankCount = unidadesSeleccionadas.Count(u => u != null && u.gameObject != null && u.gameObject.CompareTag("Tank"));
        int infantryCount = Mathf.Max(0, unidadesSeleccionadas.Count - tankCount);


        SoundColector.Instance.PlayUnitSelectionVoice(infantryCount, tankCount);
    }
}