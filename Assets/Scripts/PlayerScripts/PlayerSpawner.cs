using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("CONFIGURACIÓN DEL SPAWN")]
    public GameObject playerPrefab;

    [Header("Referencia de Cámara")]
    public cameraFollow cameraScript;

    private GameObject currentPlayer;

    void Start()
    {
        // Buscar la cámara automáticamente si no está asignada
        if (cameraScript == null)
        {
            cameraScript = FindObjectOfType<cameraFollow>();
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        // Destruir jugador anterior si existe
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        if (playerPrefab != null)
        {
            // Spawnear en la posición de ESTE GameObject (PlayerSpawner)
            currentPlayer = Instantiate(playerPrefab, transform.position, transform.rotation);

            Debug.Log($"Jugador spawnedo en: {transform.position}");

            // Asignar a la cámara
            if (cameraScript != null)
            {
                cameraScript.target = currentPlayer.transform;
                Debug.Log("Cámara asignada al jugador");
            }
        }
        else
        {
            Debug.LogError("No hay playerPrefab asignado en el inspector!");
        }
    }

    // Método para respawn
    public void RespawnPlayer()
    {
        SpawnPlayer();
    }

    // Visualizar el punto de spawn en el Editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 0.7f, "SpawnPoint.png", true);
    }
}