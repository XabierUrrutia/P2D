// Building.cs
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    private void Start()
    {
        // Inicialização do edifício
        GetComponent<SpriteRenderer>().sprite = buildingData.buildingSprite;
    }
}