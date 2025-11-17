using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Referencias de Objetivos")]
    public Transform playerBase;
    public Transform player; // Jugador principal (para compatibilidad)

    [Header("Gestión de Múltiples Jugadores")]
    private List<Transform> todosJogadores = new List<Transform>();
    private List<EnemyAI> todosEnemigos = new List<EnemyAI>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Forzar la búsqueda de la base incluso si está asignada (por si acaso)
        BuscarYAsignarBase();

        // Buscar y registrar todos los jugadores existentes
        BuscarYRegistrarTodosJogadores();
    }

    void Start()
    {
        // Registrar todos los enemigos existentes en la escena
        RegistrarEnemigosExistentes();

    }

    void BuscarYAsignarBase()
    {
        // Si ya está asignada, verificar que no esté en (0,0,0)
        if (playerBase != null && playerBase.position != Vector3.zero)
        {
            return;
        }


        GameObject baseObj = GameObject.FindGameObjectWithTag("PlayerBase");

        if (baseObj == null)
        {
            // Buscar por nombres alternativos
            string[] nombresAlternativos = { "Base", "MainBase", "Castle", "HomeBase", "PlayerBase" };
            foreach (string nombre in nombresAlternativos)
            {
                baseObj = GameObject.Find(nombre);
                if (baseObj != null) break;
            }
        }

        if (baseObj != null)
        {
            playerBase = baseObj.transform;
            Debug.Log("Base encontrada y asignada: " + playerBase.name + " en posición: " + playerBase.position);

            // Si la base está en (0,0,0), es un problema
            if (playerBase.position == Vector3.zero)
            {
                Debug.LogError("¡LA BASE ESTÁ EN (0,0,0)! Verifica la posición en la escena.");
            }
        }
        else
        {
            Debug.LogError("NO SE PUDO ENCONTRAR LA BASE DEL JUGADOR");
        }
    }

    void BuscarYRegistrarTodosJogadores()
    {
        todosJogadores.Clear();

        // Buscar todos los objetos con tag "Player"
        GameObject[] jogadoresEncontrados = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject jogadorObj in jogadoresEncontrados)
        {
            // Verificar que el jugador esté activo y tenga salud
            PlayerHealth health = jogadorObj.GetComponent<PlayerHealth>();
            if (health != null && health.enabled && jogadorObj.activeInHierarchy)
            {
                todosJogadores.Add(jogadorObj.transform);
            }
        }

        // Mantener compatibilidad con el jugador principal
        if (player == null && todosJogadores.Count > 0)
        {
            player = todosJogadores[0];
        }

    }

    void RegistrarEnemigosExistentes()
    {
        todosEnemigos.Clear();

        EnemyAI[] enemigosEncontrados = FindObjectsOfType<EnemyAI>();
        todosEnemigos.AddRange(enemigosEncontrados);

        // Notificar a cada enemigo sobre todos los jugadores
        foreach (EnemyAI enemigo in todosEnemigos)
        {
            if (enemigo != null)
            {
                foreach (Transform jogador in todosJogadores)
                {
                    enemigo.AdicionarJogador(jogador);
                }
            }
        }
    }

    // MÉTODOS PÚBLICOS PARA GESTIÓN DE JUGADORES

    /// <summary>
    /// Registra un nuevo jugador (cuando se compra)
    /// </summary>
    public void RegistrarNovoJogador(Transform novoJogador)
    {
        if (!todosJogadores.Contains(novoJogador))
        {
            todosJogadores.Add(novoJogador);

            // Notificar a todos los enemigos sobre el nuevo jugador
            foreach (EnemyAI enemy in todosEnemigos)
            {
                if (enemy != null)
                {
                    enemy.AdicionarJogador(novoJogador);
                }
            }

            Debug.Log($"Nuevo jugador registrado: {novoJogador.name}. Total: {todosJogadores.Count}");
        }
    }

    /// <summary>
    /// Remueve un jugador (cuando muere)
    /// </summary>
    public void RemoverJogador(Transform jogadorMorto)
    {
        if (todosJogadores.Contains(jogadorMorto))
        {
            todosJogadores.Remove(jogadorMorto);

            // Notificar a todos los enemigos
            foreach (EnemyAI enemy in todosEnemigos)
            {
                if (enemy != null)
                {
                    enemy.RemoverJogador(jogadorMorto);
                }
            }

            // Actualizar jugador principal si era el que murió
            if (player == jogadorMorto && todosJogadores.Count > 0)
            {
                player = todosJogadores[0];
            }
            else if (todosJogadores.Count == 0)
            {
                player = null;
            }

            Debug.Log($"Jugador removido: {jogadorMorto.name}. Total: {todosJogadores.Count}");
        }
    }

    // MÉTODOS PÚBLICOS PARA GESTIÓN DE ENEMIGOS

    /// <summary>
    /// Registra un nuevo enemigo (cuando se spawn)
    /// </summary>
    public void RegistrarEnemy(EnemyAI novoEnemy)
    {
        if (!todosEnemigos.Contains(novoEnemy))
        {
            todosEnemigos.Add(novoEnemy);

            // Pasar todos los jugadores conocidos al nuevo enemigo
            foreach (Transform jogador in todosJogadores)
            {
                if (jogador != null)
                {
                    novoEnemy.AdicionarJogador(jogador);
                }
            }

            Debug.Log($"Nuevo enemigo registrado: {novoEnemy.name}. Total: {todosEnemigos.Count}");
        }
    }

    /// <summary>
    /// Remueve un enemigo (cuando muere)
    /// </summary>
    public void RemoverEnemy(EnemyAI enemyMorto)
    {
        if (todosEnemigos.Contains(enemyMorto))
        {
            todosEnemigos.Remove(enemyMorto);
            Debug.Log($"Enemigo removido: {enemyMorto.name}. Total: {todosEnemigos.Count}");
        }
    }

    // MÉTODOS DE CONSULTA

    public List<Transform> GetTodosJogadores()
    {
        return new List<Transform>(todosJogadores);
    }

    public int GetQuantidadeJogadores()
    {
        return todosJogadores.Count;
    }

    public int GetQuantidadeEnemigos()
    {
        return todosEnemigos.Count;
    }

    public Transform GetJogadorMaisProximo(Vector3 posicao)
    {
        if (todosJogadores.Count == 0) return null;

        Transform jogadorMaisProximo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (Transform jogador in todosJogadores)
        {
            if (jogador == null || !jogador.gameObject.activeInHierarchy) continue;

            float distancia = Vector3.Distance(posicao, jogador.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                jogadorMaisProximo = jogador;
            }
        }

        return jogadorMaisProximo;
    }

    // Método público para reasignar la base si es necesario
    public void ReasignarBase(Transform nuevaBase)
    {
        playerBase = nuevaBase;
        
    }

    // Método para forzar actualización de jugadores (útil en testing)
    public void ForcarAtualizacaoJogadores()
    {
        BuscarYRegistrarTodosJogadores();
    }
}