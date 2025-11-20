using UnityEngine;
using System.Collections.Generic;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Prefabs de Edificios (Asigna los 3 tipos)")]
    public GameObject buildingType1;
    public GameObject buildingType2;
    public GameObject buildingType3;

    [Header("Configuración del Spawn")]
    public float minDistanceBetweenBuildings = 2f;
    public int maxSpawnAttempts = 50;

    [Header("Área de Spawn")]
    public Vector2 spawnAreaMin = new Vector2(-30, -30);
    public Vector2 spawnAreaMax = new Vector2(30, 30);

    [Header("Tilemap Reference")]
    public UnityEngine.Tilemaps.Tilemap groundTilemap;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        Debug.Log("[Spawner] Iniciando generación de 3 edificios...");
        SpawnBuildings();
    }

    void SpawnBuildings()
    {
        // Lista de los prefabs que debemos spawnear (uno de cada tipo)
        List<GameObject> buildingsToSpawn = new List<GameObject>();

        if (buildingType1 != null) buildingsToSpawn.Add(buildingType1);
        if (buildingType2 != null) buildingsToSpawn.Add(buildingType2);
        if (buildingType3 != null) buildingsToSpawn.Add(buildingType3);

        if (buildingsToSpawn.Count == 0)
        {
            Debug.LogError("[Spawner] ERROR: No hay prefabs de edificios asignados!");
            return;
        }

        Debug.Log($"[Spawner] Se generarán {buildingsToSpawn.Count} edificios");

        int buildingsSpawned = 0;

        foreach (GameObject buildingPrefab in buildingsToSpawn)
        {
            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < maxSpawnAttempts)
            {
                Vector3 spawnPosition = GetRandomSpawnPosition();

                if (IsValidSpawnPosition(spawnPosition))
                {
                    GameObject newBuilding = Instantiate(buildingPrefab, spawnPosition, Quaternion.identity);
                    newBuilding.name = buildingPrefab.name + "_" + (buildingsSpawned + 1);

                    spawnedPositions.Add(spawnPosition);
                    buildingsSpawned++;
                    spawned = true;

                    Debug.Log($"[Spawner] ✅ {newBuilding.name} generado en: {spawnPosition}");
                }

                attempts++;
            }

            if (!spawned)
            {
                Debug.LogError($"[Spawner] ❌ No se pudo generar {buildingPrefab.name} después de {maxSpawnAttempts} intentos");
            }
        }

        Debug.Log($"[Spawner] Generación completada. Edificios creados: {buildingsSpawned}/{buildingsToSpawn.Count}");
    }

    Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        if (groundTilemap != null)
        {
            Vector3Int cellPosition = groundTilemap.WorldToCell(spawnPos);
            if (groundTilemap.HasTile(cellPosition))
            {
                spawnPos = groundTilemap.GetCellCenterWorld(cellPosition);
            }
        }

        return spawnPos;
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        // Verificar distancia con otros edificios
        foreach (Vector3 existingPos in spawnedPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenBuildings)
            {
                return false;
            }
        }

        // Si tenemos tilemap, verificar que hay tile
        if (groundTilemap != null)
        {
            Vector3Int cellPosition = groundTilemap.WorldToCell(position);
            if (!groundTilemap.HasTile(cellPosition))
            {
                return false;
            }
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2,
            (spawnAreaMin.y + spawnAreaMax.y) / 2,
            0
        );
        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            spawnAreaMax.y - spawnAreaMin.y,
            0.1f
        );
        Gizmos.DrawWireCube(center, size);

        // Dibujar las posiciones ya generadas
        Gizmos.color = Color.red;
        foreach (Vector3 pos in spawnedPositions)
        {
            Gizmos.DrawWireSphere(pos, 0.5f);
        }
    }
}