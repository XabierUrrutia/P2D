using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Prefabs de Edificios (pequeno, medio, grande)")]
    public GameObject buildingType1; // pequeno
    public GameObject buildingType2; // medio
    public GameObject buildingType3; // grande

    [Header("Player Base (referência para distância)")]
    public Transform playerBase;

    [Header("Sites fixos de spawn (atribuir 3 Transforms no Inspector)")]
    public Transform spawnSiteA;
    public Transform spawnSiteB;
    public Transform spawnSiteC;

    [Header("Pontos de spawn de inimigos correspondentes (opcional)")]
    public Transform enemySpawnA;
    public Transform enemySpawnB;
    public Transform enemySpawnC;

    [Header("Configuração (opcionais)")]
    public Tilemap groundTilemap;
    public float minDistanceBetweenBuildings = 2f; // ainda verificado apenas como segurança
    public int maxSpawnAttempts = 50;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        Debug.Log("[Spawner] Iniciando geração condicional de 3 edifícios em sites fixos...");
        SpawnBuildingsAtSites();
    }

    void SpawnBuildingsAtSites()
    {
        // valida sites
        List<Transform> sites = new List<Transform>();
        if (spawnSiteA != null) sites.Add(spawnSiteA);
        if (spawnSiteB != null) sites.Add(spawnSiteB);
        if (spawnSiteC != null) sites.Add(spawnSiteC);

        if (sites.Count != 3)
        {
            Debug.LogError("[Spawner] É preciso atribuir exatamente 3 spawn sites (spawnSiteA/B/C) no Inspector.");
            return;
        }

        if (playerBase == null)
        {
            Debug.LogError("[Spawner] PlayerBase não atribuído (playerBase). Não é possível ordenar por proximidade.");
            return;
        }

        // ordena os sites por distância à playerBase (ascendente: mais próximo -> mais longe)
        sites.Sort((t1, t2) =>
        {
            float d1 = Vector2.SqrMagnitude((Vector2)(t1.position - playerBase.position));
            float d2 = Vector2.SqrMagnitude((Vector2)(t2.position - playerBase.position));
            return d1.CompareTo(d2);
        });

        // mapeia prefabs em ordem: pequeno (mais próximo), médio (meio), grande (mais longe)
        GameObject[] prefabs = new GameObject[3] { buildingType1, buildingType2, buildingType3 };

        for (int i = 0; i < 3; i++)
        {
            GameObject prefab = prefabs[i];
            Transform site = sites[i];

            if (prefab == null)
            {
                Debug.LogWarning($"[Spawner] prefab para índice {i} não definido. Pulando site {site.name}.");
                continue;
            }

            Vector3 spawnPos = site.position;

            // Se temos tilemap, alinhar ao centro da célula do tile
            if (groundTilemap != null)
            {
                Vector3Int cellPos = groundTilemap.WorldToCell(spawnPos);
                if (groundTilemap.HasTile(cellPos))
                {
                    spawnPos = groundTilemap.GetCellCenterWorld(cellPos);
                }
                else
                {
                    Debug.LogWarning($"[Spawner] Site '{site.name}' não tem tile no Tilemap na posição {cellPos}. Usando posição world sem snap.");
                }
            }

            // checa distância mínima com possíveis outros spawned positions (segurança)
            bool valid = true;
            foreach (var p in spawnedPositions)
            {
                if (Vector3.Distance(spawnPos, p) < minDistanceBetweenBuildings)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                Debug.LogWarning($"[Spawner] Site '{site.name}' violaria distância mínima entre edifícios. Pulando spawn aqui.");
                continue;
            }

            GameObject newBuilding = Instantiate(prefab, spawnPos, Quaternion.identity);
            newBuilding.name = prefab.name + $"_Site{i + 1}";
            spawnedPositions.Add(spawnPos);

            // cria/associa ponto de spawn de inimigo como filho do edifício para referência
            Transform enemySite = null;
            if (i == 0) enemySite = enemySpawnA;
            else if (i == 1) enemySite = enemySpawnB;
            else if (i == 2) enemySite = enemySpawnC;

            Vector3 enemyPos;
            if (enemySite != null)
            {
                enemyPos = enemySite.position;
            }
            else
            {
                // fallback: gera um ponto deslocado ligeiramente do edifício
                enemyPos = spawnPos + (Vector3.up * 1f);
            }

            GameObject esp = new GameObject("EnemySpawnPoint");
            esp.transform.position = enemyPos;
            esp.transform.SetParent(newBuilding.transform, true);

            Debug.Log($"[Spawner] ✅ {newBuilding.name} gerado em {spawnPos} com EnemySpawnPoint em {enemyPos}");
        }

        Debug.Log($"[Spawner] Geração completa. Sites utilizados: {spawnedPositions.Count}/3");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (spawnSiteA != null) Gizmos.DrawWireSphere(spawnSiteA.position, 0.4f);
        if (spawnSiteB != null) Gizmos.DrawWireSphere(spawnSiteB.position, 0.4f);
        if (spawnSiteC != null) Gizmos.DrawWireSphere(spawnSiteC.position, 0.4f);

        Gizmos.color = Color.red;
        foreach (Vector3 pos in spawnedPositions) Gizmos.DrawWireSphere(pos, 0.5f);
    }
}