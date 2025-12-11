using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FactorySpawnManager : MonoBehaviour
{
    public static FactorySpawnManager Instance;

    [Header("Referencias")]
    public Transform playerBase;  // Base del jugador para calcular distancias
    public GameObject enemyBaseGrande;  // Base enemiga grande (opcional)

    [Header("Configuración")]
    public bool enableProgressiveSpawning = true;
    public float checkInterval = 2f;

    // Lista de fábricas ordenadas por distancia
    private List<EnemyBaseFactory> allFactories = new List<EnemyBaseFactory>();
    private List<EnemyBaseFactory> conqueredFactories = new List<EnemyBaseFactory>();
    private EnemyBaseFactory currentActiveFactory = null;
    private Coroutine checkCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Encontrar todas las fábricas en la escena
        FindAllFactories();

        if (enableProgressiveSpawning)
        {
            // Ordenar por distancia a la base del jugador
            SortFactoriesByDistance();

            // Activar solo la más cercana al inicio
            ActivateClosestFactory();

            // Iniciar chequeo periódico
            checkCoroutine = StartCoroutine(CheckFactoriesStatus());
        }
        else
        {
            // Modo normal: activar todas las fábricas
            ActivateAllFactories();
        }
    }

    void FindAllFactories()
    {
        // Buscar todos los objetos con EnemyBaseFactory
        EnemyBaseFactory[] factories = FindObjectsOfType<EnemyBaseFactory>();
        allFactories = new List<EnemyBaseFactory>(factories);

        Debug.Log($"FactorySpawnManager: Encontradas {allFactories.Count} fábricas.");
    }

    void SortFactoriesByDistance()
    {
        if (playerBase == null)
        {
            Debug.LogError("FactorySpawnManager: No hay referencia a playerBase. No se pueden ordenar fábricas.");
            return;
        }

        // Ordenar por distancia a la base del jugador
        allFactories.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.transform.position, playerBase.position);
            float distB = Vector3.Distance(b.transform.position, playerBase.position);
            return distA.CompareTo(distB);
        });

        // Log para debug
        for (int i = 0; i < allFactories.Count; i++)
        {
            float distance = Vector3.Distance(allFactories[i].transform.position, playerBase.position);
            Debug.Log($"Fábrica {i}: {allFactories[i].name} a {distance:F1} unidades");
        }
    }

    void ActivateClosestFactory()
    {
        if (allFactories.Count == 0)
        {
            Debug.LogWarning("FactorySpawnManager: No hay fábricas para activar.");
            return;
        }

        // Desactivar todas las fábricas primero
        foreach (var factory in allFactories)
        {
            factory.StopSpawning();
            factory.enableSpawning = false;
        }

        // Activar solo la más cercana
        currentActiveFactory = allFactories[0];
        currentActiveFactory.enableSpawning = true;
        currentActiveFactory.TryStartSpawning();

        Debug.Log($"FactorySpawnManager: Activada fábrica más cercana: {currentActiveFactory.name}");
    }

    System.Collections.IEnumerator CheckFactoriesStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Actualizar lista de fábricas conquistadas
            UpdateConqueredFactories();

            // Verificar si la fábrica activa actual fue conquistada
            if (currentActiveFactory != null && currentActiveFactory.isConquered)
            {
                Debug.Log($"FactorySpawnManager: Fábrica {currentActiveFactory.name} conquistada. Activando siguiente...");

                // Encontrar siguiente fábrica no conquistada
                EnemyBaseFactory nextFactory = FindNextFactoryToActivate();

                if (nextFactory != null)
                {
                    // Activar siguiente fábrica
                    currentActiveFactory = nextFactory;
                    currentActiveFactory.enableSpawning = true;
                    currentActiveFactory.TryStartSpawning();
                    Debug.Log($"FactorySpawnManager: Nueva fábrica activa: {currentActiveFactory.name}");
                }
                else
                {
                    // Todas las fábricas conquistadas
                    Debug.Log("FactorySpawnManager: ¡Todas las fábricas conquistadas!");

                    // Activar base enemiga grande si existe
                    ActivateEnemyBaseGrande();

                    // Detener el chequeo
                    StopCoroutine(checkCoroutine);
                    yield break;
                }
            }
        }
    }

    void UpdateConqueredFactories()
    {
        conqueredFactories.Clear();
        foreach (var factory in allFactories)
        {
            if (factory.isConquered && !conqueredFactories.Contains(factory))
            {
                conqueredFactories.Add(factory);
            }
        }
    }

    EnemyBaseFactory FindNextFactoryToActivate()
    {
        foreach (var factory in allFactories)
        {
            if (!factory.isConquered && factory != currentActiveFactory)
            {
                return factory;
            }
        }
        return null; // Todas conquistadas
    }

    void ActivateEnemyBaseGrande()
    {
        if (enemyBaseGrande != null)
        {
            // Activar spawn en la base grande
            EnemyBaseFactory grandeFactory = enemyBaseGrande.GetComponent<EnemyBaseFactory>();
            if (grandeFactory != null)
            {
                grandeFactory.enableSpawning = true;
                grandeFactory.TryStartSpawning();
                Debug.Log("FactorySpawnManager: Base enemiga grande activada.");
            }
            else
            {
                // Si no tiene EnemyBaseFactory, activar otros componentes
                EnemySpawner spawner = enemyBaseGrande.GetComponent<EnemySpawner>();
                if (spawner != null)
                {
                    spawner.StartSpawning();
                    Debug.Log("FactorySpawnManager: Spawner de base grande activado.");
                }
            }
        }
    }

    void ActivateAllFactories()
    {
        foreach (var factory in allFactories)
        {
            factory.enableSpawning = true;
            factory.TryStartSpawning();
        }
        Debug.Log($"FactorySpawnManager: Modo normal - {allFactories.Count} fábricas activadas.");
    }

    // Método público para registrar una fábrica manualmente
    public void RegisterFactory(EnemyBaseFactory factory)
    {
        if (!allFactories.Contains(factory))
        {
            allFactories.Add(factory);

            // Si estamos en modo progresivo, reordenar
            if (enableProgressiveSpawning)
            {
                SortFactoriesByDistance();
            }
        }
    }

    // Método público para forzar la activación de una fábrica específica
    public void ForceActivateFactory(EnemyBaseFactory factory)
    {
        if (factory != null && !factory.isConquered)
        {
            // Desactivar la actual si existe
            if (currentActiveFactory != null)
            {
                currentActiveFactory.StopSpawning();
                currentActiveFactory.enableSpawning = false;
            }

            // Activar la nueva
            currentActiveFactory = factory;
            currentActiveFactory.enableSpawning = true;
            currentActiveFactory.TryStartSpawning();

            Debug.Log($"FactorySpawnManager: Fábrica forzada a activar: {factory.name}");
        }
    }

    public int GetConqueredCount()
    {
        return conqueredFactories.Count;
    }

    public int GetTotalFactories()
    {
        return allFactories.Count;
    }

    public float GetConquestProgress()
    {
        if (allFactories.Count == 0) return 0f;
        return (float)conqueredFactories.Count / allFactories.Count;
    }

    void OnDrawGizmos()
    {
        if (allFactories != null && allFactories.Count > 0)
        {
            foreach (var factory in allFactories)
            {
                if (factory != null)
                {
                    // Dibujar línea a la base del jugador
                    if (playerBase != null)
                    {
                        Gizmos.color = factory.isConquered ? Color.green :
                                     (factory == currentActiveFactory ? Color.yellow : Color.red);
                        Gizmos.DrawLine(factory.transform.position, playerBase.position);
                    }

                    // Indicador visual del estado
                    Gizmos.color = factory == currentActiveFactory ? Color.yellow :
                                 (factory.isConquered ? Color.green : Color.gray);
                    Gizmos.DrawWireSphere(factory.transform.position, 1f);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}