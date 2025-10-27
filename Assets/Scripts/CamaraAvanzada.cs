using UnityEngine;

public class CamaraAvanzada : MonoBehaviour
{
    [Header("Movimiento B�sico")]
    public float velocidadNormal = 5f;
    public float velocidadRapida = 10f;

    [Header("Movimiento con Rat�n")]
    public bool movimientoConRat�n = true;
    public float bordePantalla = 10f;
    public float velocidadRat�n = 5f;

    [Header("L�mites - Y entre -25 y 25")]
    public float limiteInferior = -25f;
    public float limiteSuperior = 25f;
    public float limiteIzquierdo = -50f;
    public float limiteDerecho = 50f;

    private Vector3 posicionObjetivo;
    private float velocidadActual;

    void Start()
    {
        posicionObjetivo = transform.position;
        // Aplicar l�mites inmediatamente
        posicionObjetivo = AplicarLimites(posicionObjetivo);
        transform.position = posicionObjetivo;
    }

    void Update()
    {
        CalcularVelocidad();
        ProcesarTeclado();
        if (movimientoConRat�n) ProcesarRat�n();
        AplicarMovimiento();
    }

    void CalcularVelocidad()
    {
        velocidadActual = Input.GetKey(KeyCode.LeftShift) ? velocidadRapida : velocidadNormal;
    }

    void ProcesarTeclado()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        posicionObjetivo += input * velocidadActual * Time.deltaTime;
        posicionObjetivo = AplicarLimites(posicionObjetivo);
    }

    void ProcesarRat�n()
    {
        Vector3 movimientoRat�n = Vector3.zero;
        Vector3 posicionRat�n = Input.mousePosition;

        if (posicionRat�n.y <= bordePantalla)
            movimientoRat�n.y -= 1;
        else if (posicionRat�n.y >= Screen.height - bordePantalla)
            movimientoRat�n.y += 1;

        posicionObjetivo += movimientoRat�n * velocidadRat�n * Time.deltaTime;
        posicionObjetivo = AplicarLimites(posicionObjetivo);
    }

    Vector3 AplicarLimites(Vector3 posicion)
    {
        return new Vector3(
            Mathf.Clamp(posicion.x, limiteIzquierdo, limiteDerecho),
            Mathf.Clamp(posicion.y, limiteInferior, limiteSuperior),
            posicion.z
        );
    }

    void AplicarMovimiento()
    {
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, 5f * Time.deltaTime);
    }

    // Debug para verificar l�mites en el build
    void OnGUI()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Posici�n: {transform.position}");
            GUI.Label(new Rect(10, 30, 300, 20), $"L�mites Y: {limiteInferior} a {limiteSuperior}");
        }
    }
}