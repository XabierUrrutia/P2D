using UnityEngine;

public class EnemyVisibilityController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool isInVisitedFog = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisibility();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("VisitedFog"))
        {
            isInVisitedFog = true;
            UpdateVisibility();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("VisitedFog"))
        {
            isInVisitedFog = false;
            UpdateVisibility();
        }
    }

    void UpdateVisibility()
    {
        // Solo se ve si NO está en la niebla visitada
        spriteRenderer.enabled = !isInVisitedFog;
    }
}