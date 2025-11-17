using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración de Detección")]
    public float alcanceDeteccao = 10f;
    public float distanciaParagemAtaque = 4f;
    public float intervaloChecagem = 0.3f;

    [Header("Referencias")]
    public Transform baseJogador;

    private EnemyController movimento;
    private EnemyShooting atirador;
    private Transform jogadorAlvo; // Jugador actual que está siendo perseguido
    private List<Transform> jogadoresDisponiveis = new List<Transform>();
    private Vector3 alvoAleatorio;
    private bool aPerseguir = false;
    private bool perseguindoJogador = false;

    // Control de tiempo
    private float proximaChecagemTime = 0f;
    private float ultimoRecalculoPerseguicao = 0f;
    private float ultimaBuscaJogadoresTime = 0f;
    private const float INTERVALO_RECALCULO_PERSEGUICAO = 1f;
    private const float INTERVALO_BUSCA_JOGADORES = 2f; // Buscar jugadores cada 2 segundos

    // Debug
    public bool debugAtivo = false;

    void Start()
    {
        movimento = GetComponent<EnemyController>();
        atirador = GetComponent<EnemyShooting>();

        BuscarReferenciasIniciais();
        BuscarTodosJogadores();
    }

    void BuscarReferenciasIniciais()
    {
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

    void BuscarTodosJogadores()
    {
        jogadoresDisponiveis.Clear();

        // Buscar todos los objetos con tag "Player"
        GameObject[] todosJogadores = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject jogadorObj in todosJogadores)
        {
            // Verificar que el jugador esté activo y tenga salud
            PlayerHealth health = jogadorObj.GetComponent<PlayerHealth>();
            if (health != null && health.enabled && jogadorObj.activeInHierarchy)
            {
                jogadoresDisponiveis.Add(jogadorObj.transform);
                if (debugAtivo) Debug.Log($"{gameObject.name}: Jugador encontrado: {jogadorObj.name}");
            }
        }

        if (debugAtivo) Debug.Log($"{gameObject.name}: Total de jugadores encontrados: {jogadoresDisponiveis.Count}");
    }

    Transform EncontrarJogadorMaisProximo()
    {
        if (jogadoresDisponiveis.Count == 0) return null;

        Transform jogadorMaisProximo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Transform jogador in jogadoresDisponiveis)
        {
            if (jogador == null || !jogador.gameObject.activeInHierarchy) continue;

            float distancia = Vector3.Distance(transform.position, jogador.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                jogadorMaisProximo = jogador;
            }
        }

        return jogadorMaisProximo;
    }

    void RemoverJogadoresInativos()
    {
        // Remover jugadores que ya no existen o están inactivos
        for (int i = jogadoresDisponiveis.Count - 1; i >= 0; i--)
        {
            if (jogadoresDisponiveis[i] == null ||
                !jogadoresDisponiveis[i].gameObject.activeInHierarchy ||
                jogadoresDisponiveis[i].GetComponent<PlayerHealth>() == null)
            {
                jogadoresDisponiveis.RemoveAt(i);
            }
        }
    }

    void Update()
    {
        // Controlar frecuencia de chequeos para mejorar rendimiento
        if (Time.time < proximaChecagemTime) return;
        proximaChecagemTime = Time.time + intervaloChecagem;

        // Buscar jugadores periódicamente
        if (Time.time - ultimaBuscaJogadoresTime >= INTERVALO_BUSCA_JOGADORES)
        {
            BuscarTodosJogadores();
            ultimaBuscaJogadoresTime = Time.time;
        }
        else
        {
            // Solo remover inactivos entre búsquedas completas
            RemoverJogadoresInativos();
        }

        // Verificar referencias de base
        if (baseJogador == null)
        {
            BuscarReferenciasIniciais();
            if (baseJogador == null) return;
        }

        // Encontrar jugador más cercano
        Transform jogadorProximo = EncontrarJogadorMaisProximo();

        if (jogadorProximo != null)
        {
            float distanciaAoJogador = Vector3.Distance(transform.position, jogadorProximo.position);

            // DEBUG: Mostrar estado actual
            if (debugAtivo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"{gameObject.name} - Distância ao jogador mais próximo: {distanciaAoJogador}, " +
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
                jogadorAlvo = jogadorProximo;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Começando a perseguir o jogador: {jogadorAlvo.name}");
            }
            else if (!jogadorDetectado && perseguindoJogador && distanciaAoJogador > alcanceDeteccao * 1.5f)
            {
                // Dejar de perseguir (con histeresis para evitar cambios bruscos)
                perseguindoJogador = false;
                aPerseguir = false;
                jogadorAlvo = null;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Parando de perseguir o jogador");
            }
            else if (perseguindoJogador && jogadorAlvo != jogadorProximo && jogadorDetectado)
            {
                // Cambiar a un jugador más cercano si es necesario
                jogadorAlvo = jogadorProximo;
                if (debugAtivo) Debug.Log($"{gameObject.name}: Mudando para jogador mais próximo: {jogadorAlvo.name}");
            }

            // Ejecutar comportamiento según el estado
            if (perseguindoJogador && jogadorAlvo != null)
            {
                PerseguirJogador(jogadorAlvo, distanciaAoJogador);
            }
            else
            {
                IrParaBase();
            }
        }
        else
        {
            // No hay jugadores disponibles, ir a la base
            if (perseguindoJogador)
            {
                perseguindoJogador = false;
                aPerseguir = false;
                jogadorAlvo = null;
            }
            IrParaBase();
        }
    }

    void PerseguirJogador(Transform jogador, float distanciaAoJogador)
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
            if (debugAtivo && Time.frameCount % 120 == 0)
            {
                Debug.Log($"{gameObject.name}: Indo para a base em {baseJogador.position}");
            }
        }
    }

    // Método público para añadir un jugador manualmente (útil cuando se crean nuevos jugadores)
    public void AdicionarJogador(Transform novoJogador)
    {
        if (!jogadoresDisponiveis.Contains(novoJogador))
        {
            jogadoresDisponiveis.Add(novoJogador);
            if (debugAtivo) Debug.Log($"{gameObject.name}: Novo jogador adicionado: {novoJogador.name}");
        }
    }

    // Método público para remover un jugador (cuando muere)
    public void RemoverJogador(Transform jogadorMorto)
    {
        if (jogadoresDisponiveis.Contains(jogadorMorto))
        {
            jogadoresDisponiveis.Remove(jogadorMorto);
            if (jogadorAlvo == jogadorMorto)
            {
                jogadorAlvo = null;
                perseguindoJogador = false;
            }
            if (debugAtivo) Debug.Log($"{gameObject.name}: Jogador removido: {jogadorMorto.name}");
        }
    }

    // Método público para verificar si está persiguiendo al jugador
    public bool EstaPersiguiendoJogador()
    {
        return perseguindoJogador;
    }

    // Método público para obtener el jugador actual
    public Transform GetJogadorAlvo()
    {
        return jogadorAlvo;
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
        if (Application.isPlaying && perseguindoJogador && jogadorAlvo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, jogadorAlvo.position);
        }

        // Dibujar líneas a todos los jugadores detectados
        Gizmos.color = Color.blue;
        foreach (Transform jogador in jogadoresDisponiveis)
        {
            if (jogador != null && jogador != jogadorAlvo)
            {
                Gizmos.DrawLine(transform.position, jogador.position);
            }
        }

        // Dibujar línea a la base si no está persiguiendo
        if (Application.isPlaying && !perseguindoJogador && baseJogador != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, baseJogador.position);
        }
    }
}