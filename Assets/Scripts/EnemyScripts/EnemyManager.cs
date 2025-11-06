using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Referencias de Objetivos")]
    public Transform playerBase;
    public Transform player;

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

        // Buscar jugador si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void BuscarYAsignarBase()
    {
        // Si ya está asignada, verificar que no esté en (0,0,0)
        if (playerBase != null && playerBase.position != Vector3.zero)
        {
            Debug.Log("Base ya asignada correctamente: " + playerBase.name + " en " + playerBase.position);
            return;
        }

        // Si no está asignada o está en (0,0,0), buscar de nuevo
        Debug.Log("Buscando base del jugador...");

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

    // Método público para reasignar la base si es necesario
    public void ReasignarBase(Transform nuevaBase)
    {
        playerBase = nuevaBase;
        Debug.Log("Base reasignada: " + playerBase.name + " en " + playerBase.position);
    }
}