using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Configuración de Selección")]
    public LayerMask capaUnidades;
    public Sprite spriteCuadroSeleccion;

    [Header("Configuración de Formación")]
    public float radioFormacion = 1.5f;
    public bool usarFormacionCircular = true;

    [Header("Tweaks de Audio")]
    [Tooltip("Tempo mínimo entre sons de movimento (em segundos).")]
    public float intervaloMinSomMovimiento = 0.15f;
    private float ultimoSomMovimientoTime = -999f;

    private Vector3 inicioArrastre;
    private Vector3 finArrastre;
    private bool arrastrando = false;
    private Camera cam;
    private List<SimpleCharacterMovement> unidadesSeleccionadas = new List<SimpleCharacterMovement>();

    private SpriteRenderer spriteRenderer;
    private GameObject cuadroObj;

    void Start()
    {
        cam = Camera.main;

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

    void ProcesarSeleccion()
    {
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
                SimpleCharacterMovement unidad = hit.collider.GetComponent<SimpleCharacterMovement>();
                if (unidad != null)
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeseleccionarTodas();
                    }

                    bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                    SeleccionarUnidad(unidad);

                    // 🔊 Som de seleção (clique simples) – só se for nova
                    if (eraNueva)
                    {
                        PlaySelectionSoundFor(unidad);
                    }

                    // Actualiza indicadores (seta) conforme nova selecção
                    UpdateSelectionIndicators();
                }
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeseleccionarTodas();
                    UpdateSelectionIndicators();
                }
            }
        }

        if (arrastrando && Input.GetMouseButton(0))
        {
            finArrastre = Input.mousePosition;
            DibujarCuadroSeleccion();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (arrastrando && Vector3.Distance(inicioArrastre, finArrastre) > 10f)
            {
                SeleccionarUnidadesEnCuadro();
                UpdateSelectionIndicators();
            }
            arrastrando = false;
            OcultarCuadroSeleccion();
        }
    }

    void ProcesarMovimiento()
    {
        if (Input.GetMouseButtonDown(1) && unidadesSeleccionadas.Count > 0)
        {
            Vector3 destino = cam.ScreenToWorldPoint(Input.mousePosition);
            destino.z = 0;

            // Calcular posiciones distribuidas
            Vector3[] destinos = CalcularDestinosDistribuidos(destino, unidadesSeleccionadas.Count);

            for (int i = 0; i < unidadesSeleccionadas.Count; i++)
            {
                if (unidadesSeleccionadas[i] != null)
                {
                    unidadesSeleccionadas[i].MoverADestino(destinos[i]);
                }
            }

            // 🔊 Som de movimento com cooldown (sem verificar tag "Tank")
            if (SoundColector.Instance != null &&
                Time.time - ultimoSomMovimientoTime >= intervaloMinSomMovimiento)
            {
                // toca um som genérico de movimento (infantry) para evitar dependência de tags
                SoundColector.Instance.PlayInfantryMove();
                ultimoSomMovimientoTime = Time.time;
            }
        }
    }

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
            // Formación circular alrededor del punto
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
            // Formación en cuadrícula
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

    void SeleccionarUnidadesEnCuadro()
    {
        Vector2 min = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Min(inicioArrastre.x, finArrastre.x),
            Mathf.Min(inicioArrastre.y, finArrastre.y),
            0));

        Vector2 max = cam.ScreenToWorldPoint(new Vector3(
            Mathf.Max(inicioArrastre.x, finArrastre.x),
            Mathf.Max(inicioArrastre.y, finArrastre.y),
            0));

        Collider2D[] unidadesEnArea = Physics2D.OverlapAreaAll(min, max, capaUnidades);

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeseleccionarTodas();
        }

        SimpleCharacterMovement primeraNueva = null;

        foreach (Collider2D collider in unidadesEnArea)
        {
            SimpleCharacterMovement unidad = collider.GetComponent<SimpleCharacterMovement>();
            if (unidad != null)
            {
                bool eraNueva = !unidadesSeleccionadas.Contains(unidad);
                SeleccionarUnidad(unidad);

                if (eraNueva && primeraNueva == null)
                {
                    primeraNueva = unidad;
                }
            }
        }

        // 🔊 Som de seleção (apenas 1 por arrasto)
        if (primeraNueva != null)
        {
            PlaySelectionSoundFor(primeraNueva);
        }

        Debug.Log($"Unidades seleccionadas: {unidadesSeleccionadas.Count}");

        // Atualiza indicadores após seleção por arrasto
        UpdateSelectionIndicators();
    }

    void SeleccionarUnidad(SimpleCharacterMovement unidad)
    {
        if (!unidadesSeleccionadas.Contains(unidad))
        {
            unidadesSeleccionadas.Add(unidad);
            unidad.Seleccionar();
            Debug.Log($"Unidad seleccionada: {unidad.name}");
        }

        // Atualiza indicadores sempre que adicionas uma unidade
        UpdateSelectionIndicators();
    }

    void DeseleccionarUnidad(SimpleCharacterMovement unidad)
    {
        if (unidadesSeleccionadas.Contains(unidad))
        {
            unidadesSeleccionadas.Remove(unidad);
            unidad.Deseleccionar();
        }

        // Atualiza indicadores quando removes
        UpdateSelectionIndicators();
    }

    void DeseleccionarTodas()
    {
        foreach (SimpleCharacterMovement unidad in unidadesSeleccionadas)
        {
            if (unidad != null)
            {
                unidad.Deseleccionar();
            }
        }
        unidadesSeleccionadas.Clear();

        // Atualiza indicadores após limpar seleção
        UpdateSelectionIndicators();
    }

    void DibujarCuadroSeleccion()
    {
        Vector3 inicioMundo = cam.ScreenToWorldPoint(new Vector3(inicioArrastre.x, inicioArrastre.y, 0));
        Vector3 finMundo = cam.ScreenToWorldPoint(new Vector3(finArrastre.x, finArrastre.y, 0));

        inicioMundo.z = 0;
        finMundo.z = 0;

        Vector3 centro = (inicioMundo + finMundo) / 2f;
        Vector3 tamaño = new Vector3(
            Mathf.Abs(finMundo.x - inicioMundo.x),
            Mathf.Abs(finMundo.y - inicioMundo.y),
            1f
        );

        cuadroObj.transform.position = centro;
        cuadroObj.transform.localScale = tamaño;

        cuadroObj.SetActive(true);
    }

    void OcultarCuadroSeleccion()
    {
        cuadroObj.SetActive(false);
    }

    // ---------- HELPER DE ÁUDIO ----------

    void PlaySelectionSoundFor(SimpleCharacterMovement unidad)
    {
        if (SoundColector.Instance == null || unidad == null)
            return;

        // removed tag check, always play infantry selection sound
        SoundColector.Instance.PlayInfantrySelect();
    }

    // ----------------- ADIÇÃO: controlar indicador visual (seta) -----------------
    // Mostra indicador apenas se EXACTAMENTE 1 unidade do jogador estiver selecionada.
    void UpdateSelectionIndicators()
    {
        // Se exactamente uma selecionada e não for inimigo, mostrar indicador nessa unidade só
        if (unidadesSeleccionadas.Count == 1 && unidadesSeleccionadas[0] != null && !unidadesSeleccionadas[0].CompareTag("Enemy"))
        {
            SimpleCharacterMovement sole = unidadesSeleccionadas[0];

            // Assegura que o indicador está referenciado no componente da unidade
            EnsureIndicatorAssigned(sole);

            // Desactivar indicador em todas as unidades e activar só na escolhida
            foreach (var u in FindObjectsOfType<SimpleCharacterMovement>())
            {
                if (u == null) continue;
                if (u.indicadorSeleccion != null)
                    u.indicadorSeleccion.SetActive(u == sole);
            }
        }
        else
        {
            // Esconder todos os indicadores
            foreach (var u in FindObjectsOfType<SimpleCharacterMovement>())
            {
                if (u == null) continue;
                if (u.indicadorSeleccion != null)
                    u.indicadorSeleccion.SetActive(false);
            }
        }
    }

    // Tenta localizar e associar um indicador (child com FloatingArrow ou com nomes comuns) à unidade,
    // caso o campo public 'indicadorSeleccion' esteja vazio no prefab/instância.
    void EnsureIndicatorAssigned(SimpleCharacterMovement unidad)
    {
        if (unidad == null) return;
        if (unidad.indicadorSeleccion != null) return;

        // procura componente FloatingArrow em filhos
        var floating = unidad.GetComponentInChildren<FloatingArrow>(true);
        if (floating != null)
        {
            unidad.indicadorSeleccion = floating.gameObject;
            unidad.indicadorSeleccion.SetActive(false);
            unidad.indicadorSeleccion.transform.SetParent(unidad.transform, true);
            unidad.indicadorSeleccion.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            return;
        }

        // procura por nomes comuns
        Transform t = unidad.transform.Find("SelectionArrow") ?? unidad.transform.Find("indicadorSeleccion") ?? unidad.transform.Find("Arrow");
        if (t != null)
        {
            unidad.indicadorSeleccion = t.gameObject;
            unidad.indicadorSeleccion.SetActive(false);
            unidad.indicadorSeleccion.transform.SetParent(unidad.transform, true);
            unidad.indicadorSeleccion.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        }
    }
}