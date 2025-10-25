using UnityEngine;

public class CharacterMovement2D : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 4f;
    public float acceleration = 8f;
    public float deceleration = 10f;
    public float stopDistance = 0.05f;

    [Header("Camadas de terreno")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;

    [Header("Sprites - 8 direções (2 frames + idle)")]
    public Sprite frontRight_L;
    public Sprite frontRight_R;
    public Sprite frontRight_Idle;

    public Sprite frontLeft_L;
    public Sprite frontLeft_R;
    public Sprite frontLeft_Idle;

    public Sprite backRight_L;
    public Sprite backRight_R;
    public Sprite backRight_Idle;

    public Sprite backLeft_L;
    public Sprite backLeft_R;
    public Sprite backLeft_Idle;

    [Header("Click Marker")]
    public GameObject clickMarkerPrefab;

    private SpriteRenderer sr;
    private Camera cam;
    private Vector3 targetPos;
    private bool isMoving = false;
    private float currentSpeed = 0f;
    private float animTimer = 0f;
    private bool animToggle = false;
    private Vector2 moveDir = Vector2.zero;
    private Vector2 lastDir = new Vector2(1, -1); // Direção inicial visível

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;

        // Restaurar posição guardada (se existir)
        if (PlayerPositionManager.HasSavedPosition)
        {
            transform.position = PlayerPositionManager.GetPosition();
            PlayerPositionManager.HasSavedPosition = false;
            Debug.Log("Posição restaurada: " + transform.position);
        }

        targetPos = transform.position;
        UpdateSprite(lastDir, false, true);
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        UpdateAnimation();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;

            if (!IsInWater(mouseWorld) && IsOnGround(mouseWorld))
            {
                targetPos = mouseWorld;
                isMoving = true;

                if (clickMarkerPrefab != null)
                {
                    GameObject marker = Instantiate(clickMarkerPrefab, targetPos, Quaternion.identity);
                    Destroy(marker, 0.6f);
                }
            }
        }
    }

    void HandleMovement()
    {
        if (!isMoving) return;

        Vector3 dir = (targetPos - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, targetPos);

        Vector3 nextPos = transform.position + dir * moveSpeed * Time.deltaTime;
        if (IsInWater(nextPos))
        {
            isMoving = false;
            return;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, acceleration * Time.deltaTime);
        moveDir = dir;
        transform.position += (Vector3)(moveDir * currentSpeed * Time.deltaTime);

        if (dist < stopDistance)
        {
            isMoving = false;
        }
    }

    void UpdateAnimation()
    {
        if (moveDir.magnitude > 0.1f && isMoving)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.2f)
            {
                animTimer = 0f;
                animToggle = !animToggle;
            }

            UpdateSprite(moveDir, animToggle);
            lastDir = moveDir;
        }
        else
        {
            UpdateSprite(lastDir, false, true);
        }
    }

    void UpdateSprite(Vector2 direction, bool toggle, bool idle = false)
    {
        if (sr == null) return;

        // ângulo entre 0 e 360
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        Sprite chosen = frontRight_Idle;

        // Ajuste dos ângulos para evitar "virar de costas" em pequenos desvios
        if (angle >= 15f && angle < 75f)
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);
        else if (angle >= 75f && angle < 105f)
            chosen = idle ? backRight_Idle : (toggle ? backRight_L : backRight_R);
        else if (angle >= 105f && angle < 165f)
            chosen = idle ? backLeft_Idle : (toggle ? backLeft_L : backLeft_R);
        else if (angle >= 165f && angle < 195f)
            chosen = idle ? frontLeft_Idle : (toggle ? frontLeft_L : frontLeft_R);
        else if (angle >= 195f && angle < 255f)
            chosen = idle ? frontLeft_Idle : (toggle ? frontLeft_L : frontLeft_R);
        else if (angle >= 255f && angle < 315f)
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);
        else // 315 → 360 / 0 → 15
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);

        sr.sprite = chosen;
    }

    bool IsInWater(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, waterLayer) != null;
    }

    bool IsOnGround(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, groundLayer) != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPos, 0.1f);
    }
}
