using UnityEngine;

[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración de Detección")]
    public float alcanceDeteccao = 10f;
    public float distanciaParagemAtaque = 4f;
    public float intervaloChecagem = 0.3f; // Reducir frecuencia de chequeos

    [Header("Referencias")]
    public Transform baseJogador; // Ahora puedes asignar manualmente desde el inspector

    private EnemyController movimento;
    private EnemyShooting atirador;
    private Transform jogador;
    private Vector3 alvoAleatorio;
    private bool aPerseguir = false;
    private bool perseguindoJogador = false;

    // Control de tiempo
    private float proximaChecagemTime = 0f;
    private float ultimoRecalculoPerseguicao = 0f;
    private const float INTERVALO_RECALCULO_PERSEGUICAO = 1f;

    // Debug
    private bool debugAtivo = true;

    void Start()
    {
        movimento = GetComponent<EnemyController>();
        atirador = GetComponent<EnemyShooting>();

        // Buscar referencias
        BuscarReferenciasIniciais();

        // Si no encontramos la base, mostrar error
        if (baseJogador == null)
        {
            Debug.LogError($"{gameObject.name}: Não foi possível encontrar a base do jogador!");
        }

        if (jogador == null)
        {
            Debug.LogError($"{gameObject.name}: Não foi possível encontrar o jogador!");
        }
    }

    void BuscarReferenciasIniciais()
    {
        // Buscar jogador por tag
        GameObject jogadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jogadorObj != null)
        {
            jogador = jogadorObj.transform;
            if (debugAtivo) Debug.Log($"{gameObject.name}: Jogador encontrado: {jogador.name}");
        }

        // Buscar base - primero intentar referencia manual, luego por tag
        if (baseJogador == null)
        {
            GameObject baseObj = GameObject.FindGameObjectWithTag("PlayerBase");
            if (baseObj != null)
            {
                baseJogador = baseObj.transform;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Base encontrada: {baseJogador.name}");
            }
        }

        // Si todavía no tenemos base, intentar con EnemyManager
        if (baseJogador == null && EnemyManager.Instance != null)
        {
            baseJogador = EnemyManager.Instance.playerBase;
            if (debugAtivo && baseJogador != null)
                Debug.Log($"{gameObject.name}: Base encontrada via EnemyManager: {baseJogador.name}");
        }
    }

    void Update()
    {
        // Controlar frecuencia de chequeos para mejorar rendimiento
        if (Time.time < proximaChecagemTime) return;
        proximaChecagemTime = Time.time + intervaloChecagem;

        // Verificar y buscar referencias si es necesario
        if (jogador == null || baseJogador == null)
        {
            BuscarReferenciasIniciais();
            if (jogador == null || baseJogador == null) return;
        }

        // Calcular distancia al jugador
        float distanciaAoJogador = Vector3.Distance(transform.position, jogador.position);

        // DEBUG: Mostrar estado actual
        if (debugAtivo && Time.frameCount % 60 == 0) // Mostrar cada ~1 segundo
        {
            Debug.Log($"{gameObject.name} - Distância ao jogador: {distanciaAoJogador}, " +
                     $"Alcance: {alcanceDeteccao}, Perseguindo: {perseguindoJogador}");
        }

        // Verificar detección del jugador
        bool jogadorDetectado = distanciaAoJogador <= alcanceDeteccao;

        // Actualizar estado de persecución
        if (jogadorDetectado && !perseguindoJogador)
        {
            // Comenzar a perseguir
            perseguindoJogador = true;
            aPerseguir = true;
            if (debugAtivo) Debug.Log($"{gameObject.name}: Começando a perseguir o jogador!");
        }
        else if (!jogadorDetectado && perseguindoJogador && distanciaAoJogador > alcanceDeteccao * 1.5f)
        {
            // Dejar de perseguir (con histeresis para evitar cambios bruscos)
            perseguindoJogador = false;
            aPerseguir = false;
            if (debugAtivo) Debug.Log($"{gameObject.name}: Parando de perseguir o jogador");
        }

        // Ejecutar comportamiento según el estado
        if (perseguindoJogador)
        {
            PerseguirJogador(distanciaAoJogador);
        }
        else
        {
            IrParaBase();
        }
    }

    void PerseguirJogador(float distanciaAoJogador)
    {
        if (distanciaAoJogador > distanciaParagemAtaque)
        {
            // Perseguir al jugador - recalculando ruta periódicamente
            if (Time.time - ultimoRecalculoPerseguicao >= INTERVALO_RECALCULO_PERSEGUICAO)
            {
                if (debugAtivo) Debug.Log($"{gameObject.name}: Perseguindo jogador para {jogador.position}");
                movimento.SetTarget(jogador.position);
                ultimoRecalculoPerseguicao = Time.time;
            }
        }
        else
        {
            // Parar para disparar al jugador
            if (debugAtivo) Debug.Log($"{gameObject.name}: Parando para atirar no jogador");
            movimento.StopMoving();
        }
    }

    void IrParaBase()
    {
        if (baseJogador != null)
        {
            movimento.SetTarget(baseJogador.position);
            if (debugAtivo && Time.frameCount % 120 == 0) // Mostrar cada ~2 segundos
            {
                Debug.Log($"{gameObject.name}: Indo para a base em {baseJogador.position}");
            }
        }
    }

    // Método público para verificar si está persiguiendo al jugador
    public bool EstaPersiguiendoJogador()
    {
        return perseguindoJogador;
    }

    // Método público para forzar la persecución (útil para testing)
    public void ForcarPerseguicao(bool perseguir)
    {
        perseguindoJogador = perseguir;
        aPerseguir = perseguir;
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar alcance de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceDeteccao);

        // Dibujar distancia de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaParagemAtaque);

        // Dibujar línea al jugador si está siendo perseguido
        if (Application.isPlaying && perseguindoJogador && jogador != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, jogador.position);
        }

        // Dibujar línea a la base si no está persiguiendo
        if (Application.isPlaying && !perseguindoJogador && baseJogador != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, baseJogador.position);
        }
    }

    // Método para debug en tiempo de ejecución
    void OnGUI()
    {
        if (debugAtivo && Application.isPlaying)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = perseguindoJogador ? Color.red : Color.white;
            style.fontSize = 12;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            string status = perseguindoJogador ? "PERSEGUINDO" : "INDO PARA BASE";
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 50), status, style);
        }
    }
}