using UnityEngine;

public class soldierMovement : MonoBehaviour
{
    public float speed = 5f;
    public LayerMask groundLayer;

    [Header("Sprites de Direcci�n")]
    public Sprite frontRightSprite;
    public Sprite frontLeftSprite;
    public Sprite backRightSprite;
    public Sprite backLeftSprite;

    private Camera cam;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastDirection = Vector2.zero;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        targetPosition = transform.position;

        // Establecer sprite inicial
        if (spriteRenderer != null && frontRightSprite != null)
        {
            spriteRenderer.sprite = frontRightSprite;
        }

        targetPosition = transform.position;

        // Restaurar posi��o guardada (se existir)
        if (PlayerPositionManager.HasSavedPosition)
        {
            transform.position = PlayerPositionManager.GetPosition();
            targetPosition = transform.position;
            PlayerPositionManager.HasSavedPosition = false; // evita reposicionar de novo se recarregares
            Debug.Log("Posi��o restaurada: " + transform.position);
        }
    }

    void Update()
    {
        // Detecci�n del clic
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // Lanzar raycast
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity, groundLayer);

            if (hit.collider != null)
            {
                targetPosition = hit.point;
                isMoving = true;
            }
        }

        // Actualizar direcci�n incluso cuando no se mueve
        if (!isMoving && lastDirection != Vector2.zero)
        {
            UpdateSpriteDirection(lastDirection);
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector2 moveDirection = (targetPosition - transform.position).normalized;

            // Actualizar la direcci�n
            if (moveDirection.magnitude > 0.1f)
            {
                UpdateSpriteDirection(moveDirection);
                lastDirection = moveDirection;
            }

            // Mover el personaje
            Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            // Verificar si lleg� al destino
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMoving = false;
            }
        }
    }

    void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null) return;

        // Determinar la direcci�n basada en el vector de movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Normalizar el �ngulo a 0-360
        if (angle < 0) angle += 360;

        // Determinar el sprite basado en el �ngulo
        if (angle >= 45 && angle < 135)
        {
            // Arriba - Espalda
            if (direction.x >= 0)
                spriteRenderer.sprite = backRightSprite;
            else
                spriteRenderer.sprite = backLeftSprite;
        }
        else if (angle >= 135 && angle < 225)
        {
            // Izquierda - Frente lateral izquierdo
            spriteRenderer.sprite = frontLeftSprite;
        }
        else if (angle >= 225 && angle < 315)
        {
            // Abajo - Frente
            if (direction.x >= 0)
                spriteRenderer.sprite = frontRightSprite;
            else
                spriteRenderer.sprite = frontLeftSprite;
        }
        else
        {
            // Derecha - Frente lateral derecho
            spriteRenderer.sprite = frontRightSprite;
        }
    }
}