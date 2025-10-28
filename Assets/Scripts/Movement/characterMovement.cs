using System.Collections.Generic;
using System.Linq;
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
    // Opcional: definir uma layer para pontes (também pode ser ground mas ter layer dedicada facilita)
    public LayerMask bridgeLayer;

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
    // Waypoints para quando precisamos de contornar água (ex: ir até uma ponte primeiro)
    private List<Vector3> waypoints = new List<Vector3>();
    private float waypointReachThreshold = 0.12f;
    // Limites para procura de ponte
    public float bridgeSearchMaxRadius = 20f;
    public float bridgeSearchStep = 1f;

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

            // Só permitir clicar em pontos em ground (ou ponte)
            if (IsOnGround(mouseWorld))
            {
                // Primeiro verificamos se a linha direta atravessa água (ground -> water -> ground)
                bool crosses = PathCrossesWater(transform.position, mouseWorld);

                waypoints.Clear();

                if (crosses)
                {
                    // Tentamos encontrar uma ponte alcançável
                    Vector3? bridgePoint = FindNearestBridge(transform.position);
                    if (bridgePoint.HasValue)
                    {
                        // adiciona waypoint: ir à ponte primeiro, depois ao destino
                        waypoints.Add(bridgePoint.Value);
                        waypoints.Add(mouseWorld);
                        targetPos = waypoints[0];
                        isMoving = true;
                    }
                    else
                    {
                        // sem ponte conhecida: não mover, deixa o jogador clicar noutro sítio
                        Debug.Log("Caminho atravessa água e nenhuma ponte foi encontrada nas proximidades.");
                        isMoving = false;
                    }
                }
                else
                {
                    // caminho direto sem problema
                    waypoints.Add(mouseWorld);
                    targetPos = waypoints[0];
                    isMoving = true;
                }

                if (isMoving && clickMarkerPrefab != null)
                {
                    GameObject marker = Instantiate(clickMarkerPrefab, waypoints.Last(), Quaternion.identity);
                    Destroy(marker, 0.6f);
                }
            }
        }
    }
    public void SetTarget(Vector3 pos)
    {
        if (!IsInWater(pos) && IsOnGround(pos))
        {
            targetPos = pos;
            isMoving = true;
        }
    }
    void HandleMovement()
    {
        if (!isMoving || waypoints.Count == 0) return;

        // Mantemos um waypoint atual (o primeiro da lista)
        Vector3 currentTarget = waypoints[0];
        Vector3 toTarget = currentTarget - transform.position;
        float dist = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;

        // Prever próximo passo e evitar entrar em água
        Vector3 nextPos = transform.position + dir * moveSpeed * Time.deltaTime;
        if (IsInWater(nextPos))
        {
            // se encontramos água inesperada no caminho, tentamos procurar ponte e refazer waypoints
            Vector3? bridgePoint = FindNearestBridge(transform.position);
            if (bridgePoint.HasValue)
            {
                waypoints.Insert(0, bridgePoint.Value);
                currentTarget = waypoints[0];
                dir = (currentTarget - transform.position).normalized;
            }
            else
            {
                // sem ponte: paramos o movimento
                isMoving = false;
                currentSpeed = 0f;
                return;
            }
        }

        // Suavizar velocidade: acelerar quando distante, desacelerar perto
        float desiredSpeed = moveSpeed;
        if (dist < 0.5f)
        {
            // desaceleração suave quando se aproxima do waypoint
            desiredSpeed = Mathf.Lerp(0f, moveSpeed, dist / 0.5f);
        }

        float accel = desiredSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * Time.deltaTime);

        moveDir = dir;
        transform.position += (Vector3)(moveDir * currentSpeed * Time.deltaTime);

        // Verificar chegada ao waypoint
        if (dist < Mathf.Max(stopDistance, waypointReachThreshold))
        {
            // chegámos ao waypoint atual
            waypoints.RemoveAt(0);
            if (waypoints.Count > 0)
            {
                targetPos = waypoints[0];
            }
            else
            {
                isMoving = false;
                currentSpeed = 0f;
            }
        }
    }

    // Verifica se ao longo do segmento existe um padrão ground -> water -> ground
    bool PathCrossesWater(Vector3 start, Vector3 end)
    {
        float totalDist = Vector2.Distance(start, end);
        if (totalDist < 0.01f) return false;

        float step = 0.12f; // amostragem cada 0.12 unidades
        bool seenGround = false;
        bool seenWaterAfterGround = false;

        int steps = Mathf.CeilToInt(totalDist / step);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / (float)steps;
            Vector3 p = Vector3.Lerp(start, end, t);

            bool g = IsOnGround(p);
            bool w = IsInWater(p);

            if (g && !seenGround)
                seenGround = true;

            if (seenGround && w)
                seenWaterAfterGround = true;

            if (seenWaterAfterGround && g)
            {
                // encontrou ground -> water -> ground
                return true;
            }
        }

        return false;
    }

    // Procura o ponto mais próximo que pertença a bridgeLayer e cuja rota até ele não cruza água
    Vector3? FindNearestBridge(Vector3 from)
    {
        float radius = bridgeSearchStep;
        Collider2D[] hits = null;
        while (radius <= bridgeSearchMaxRadius)
        {
            hits = Physics2D.OverlapCircleAll(from, radius, bridgeLayer);
            if (hits != null && hits.Length > 0)
                break;

            radius += bridgeSearchStep;
        }

        if (hits == null || hits.Length == 0)
            return null;

        // Ordenar por distância e escolher primeiro que tenha caminho sem cruzar água
        var ordered = hits.OrderBy(h => Vector2.Distance(from, h.bounds.center));
        foreach (var h in ordered)
        {
            Vector3 candidate = h.bounds.center;
            // se há um ponto do collider mais próximo ao 'from', use esse
            Vector3 closest = h.ClosestPoint(from);
            if (closest != Vector3.zero)
                candidate = closest;

            // verificar se o caminho até candidate cruza água
            if (!PathCrossesWater(from, candidate))
            {
                return candidate;
            }
        }

        return null;
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
