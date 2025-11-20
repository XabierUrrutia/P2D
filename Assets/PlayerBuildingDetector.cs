using UnityEngine;
using System.Collections.Generic;

public class PlayerBuildingDetector : MonoBehaviour
{
    [Header("Configuración")]
    public float detectionRange = 5f;

    private List<Building> allBuildings = new List<Building>();
    private Building currentBuilding = null;

    void Start()
    {
        Building[] buildingsArray = FindObjectsOfType<Building>();
        allBuildings = new List<Building>(buildingsArray);
        Debug.Log($"[{gameObject.name}] Encontró {allBuildings.Count} edificios");
    }

    void Update()
    {
        CheckBuildingsInRange();
    }

    void CheckBuildingsInRange()
    {
        Building closestBuilding = null;
        float closestDistance = Mathf.Infinity;

        foreach (Building building in allBuildings)
        {
            if (building == null || building.isConquered) continue;

            float distance = Vector2.Distance(transform.position, building.transform.position);

            if (distance <= building.conquestRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestBuilding = building;
            }
        }

        // Si cambió el edificio más cercano
        if (currentBuilding != closestBuilding)
        {
            // Desregistrarse del edificio anterior
            if (currentBuilding != null)
            {
                currentBuilding.UnregisterPlayer(this);
                Debug.Log($"[{gameObject.name}] Dejó de conquistar: {currentBuilding.gameObject.name}");
            }

            // Registrarse en el nuevo edificio
            currentBuilding = closestBuilding;
            if (currentBuilding != null)
            {
                currentBuilding.RegisterPlayer(this);
                Debug.Log($"[{gameObject.name}] Empezó a conquistar: {currentBuilding.gameObject.name}");
            }
        }

        // Si no hay edificios en rango, asegurarse de que currentBuilding es null
        if (closestBuilding == null && currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
            currentBuilding = null;
        }
    }

    // Importante: limpiar cuando el jugador se destruye
    void OnDestroy()
    {
        if (currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
        }
    }

    void OnDisable()
    {
        if (currentBuilding != null)
        {
            currentBuilding.UnregisterPlayer(this);
        }
    }
}