// BuildingData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "RTS/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public Sprite buildingSprite;
    public GameObject buildingPrefab;
    public int cost;
    public Vector2 gridSize = Vector2.one;
}