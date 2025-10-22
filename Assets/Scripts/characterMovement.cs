using UnityEngine;
using System.Collections;

public class characterMovement : MonoBehaviour
{
    public float speed = 5f;
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public float checkDistance = 1f;

    [Header("Sprites de Dirección")]
    public Sprite frontRightSprite;
    public Sprite frontLeftSprite;
    public Sprite backRightSprite;
    public Sprite backLeftSprite;

    private Camera cam;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Rigidbody2D rb;
    private Vector3[] path;
    private int currentWaypoint;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastDirection = Vector2.zero;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetPosition = transform.position;

        // Establecer sprite inicial
        if (spriteRenderer != null && frontRightSprite != null)
        {
            spriteRenderer.sprite = frontRightSprite;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            if (!IsInWater(mouseWorldPos) && IsOnGround(mouseWorldPos))
            {
                targetPosition = mouseWorldPos;

                // Calcular ruta simple
                Vector3[] simplePath = CalculateSimplePath(transform.position, targetPosition);
                if (simplePath != null && simplePath.Length > 0)
                {
                    path = simplePath;
                    currentWaypoint = 0;
                    isMoving = true;
                    Debug.Log("Iniciando movimiento con " + path.Length + " waypoints");
                }
            }
        }

        // Actualizar dirección incluso cuando no se mueve
        if (!isMoving && lastDirection != Vector2.zero)
        {
            UpdateSpriteDirection(lastDirection);
        }
    }

    void FixedUpdate()
    {
        if (isMoving && path != null && currentWaypoint < path.Length)
        {
            // Moverse hacia el waypoint actual
            Vector3 currentTarget = path[currentWaypoint];
            Vector2 moveDirection = (currentTarget - transform.position).normalized;

            // Actualizar la dirección del sprite
            if (moveDirection.magnitude > 0.1f)
            {
                UpdateSpriteDirection(moveDirection);
                lastDirection = moveDirection;
            }

            Vector2 newPosition = Vector2.MoveTowards(rb.position, currentTarget, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            // Verificar si llegó al waypoint
            if (Vector2.Distance(rb.position, currentTarget) < 0.1f)
            {
                currentWaypoint++;

                // Si terminó la ruta
                if (currentWaypoint >= path.Length)
                {
                    isMoving = false;
                    path = null;
                    Debug.Log("Llegó al destino final");
                }
            }
        }
    }

    void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null) return;

        // Determinar la dirección basada en el vector de movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Normalizar el ángulo a 0-360
        if (angle < 0) angle += 360;

        // Determinar el sprite basado en el ángulo
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

    Vector3[] CalculateSimplePath(Vector3 start, Vector3 end)
    {
        // Verificar si el camino directo es válido
        if (IsPathClear(start, end))
        {
            return new Vector3[] { end };
        }

        // Buscar un punto intermedio
        Vector3 intermediate = FindIntermediatePoint(start, end);
        if (intermediate != start)
        {
            return new Vector3[] { intermediate, end };
        }

        return null;
    }

    bool IsPathClear(Vector3 start, Vector3 end)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        // Verificar agua en el camino
        RaycastHit2D waterHit = Physics2D.Raycast(start, direction, distance, waterLayer);
        return waterHit.collider == null;
    }

    Vector3 FindIntermediatePoint(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;

        // Probar diferentes distancias
        for (float dist = 1f; dist <= 3f; dist += 0.5f)
        {
            // Probar diferentes ángulos
            for (int angle = -45; angle <= 45; angle += 15)
            {
                Vector3 rotatedDir = Quaternion.Euler(0, 0, angle) * direction;
                Vector3 testPoint = start + rotatedDir * dist;

                if (IsOnGround(testPoint) && !IsInWater(testPoint) &&
                    IsPathClear(start, testPoint) && IsPathClear(testPoint, end))
                {
                    return testPoint;
                }
            }
        }

        return start; // No se encontró punto intermedio
    }

    bool IsInWater(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.3f, waterLayer) != null;
    }

    bool IsOnGround(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.3f, groundLayer) != null;
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
                Gizmos.DrawWireSphere(path[i], 0.1f);
            }
            if (path.Length > 0)
            {
                Gizmos.DrawWireSphere(path[path.Length - 1], 0.1f);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.2f);

        // Dibujar dirección actual
        if (lastDirection != Vector2.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, lastDirection);
        }
    }
}