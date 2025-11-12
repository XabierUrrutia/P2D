// BuildingManager.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private Material ghostMaterial;

    private BuildingData selectedBuilding;
    private GameObject ghostBuilding;
    private SpriteRenderer ghostRenderer;
    private bool isPlacingBuilding = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isPlacingBuilding) return;

        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            TryPlaceBuilding();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuilding();
        }
    }

    public void SelectBuilding(BuildingData buildingData)
    {
        selectedBuilding = buildingData;
        CreateGhostBuilding();
        isPlacingBuilding = true;
    }

    private void CreateGhostBuilding()
    {
        if (ghostBuilding != null)
            Destroy(ghostBuilding);

        ghostBuilding = new GameObject("GhostBuilding");
        ghostRenderer = ghostBuilding.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = selectedBuilding.buildingSprite;
        ghostRenderer.material = ghostMaterial;
        ghostRenderer.color = new Color(1, 1, 1, 0.7f);
    }

    private void UpdateGhostPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        // Opcional: Snap to grid
        worldPos.x = Mathf.Round(worldPos.x);
        worldPos.y = Mathf.Round(worldPos.y);

        ghostBuilding.transform.position = worldPos;
    }

    private void TryPlaceBuilding()
    {
        Vector2 placePosition = ghostBuilding.transform.position;

        // Verifica se a posição é válida
        if (IsPositionValid(placePosition))
        {
            Instantiate(selectedBuilding.buildingPrefab, placePosition, Quaternion.identity);
            // Aqui você pode subtrair recursos etc
        }
    }

    private bool IsPositionValid(Vector2 position)
    {
        // Verifica colisão com outros edifícios
        Collider2D[] colliders = Physics2D.OverlapBoxAll(position,
            selectedBuilding.gridSize, 0);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Building"))
                return false;
        }

        return true;
    }

    private void CancelBuilding()
    {
        isPlacingBuilding = false;
        if (ghostBuilding != null)
            Destroy(ghostBuilding);
    }
}