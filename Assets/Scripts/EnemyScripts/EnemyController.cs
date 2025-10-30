using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 3f;
    public float acceleration = 6f;
    public float deceleration = 10f;
    public float stopDistance = 0.05f;
    public float patrolRadius = 10f;
    public float waitTimeAtPoint = 2f;

    [Header("Deteção e Ataque")]
    public float detectionRange = 7f;
    public float attackRange = 5f;
    public float shootCooldown = 1.2f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    // Novos parâmetros para bala
    [Header("Bala")]
    public float bulletSpeed = 8f;
    public int bulletDamage = 1;

    [Header("Camadas de terreno")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;
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

    private SpriteRenderer sr;
    private Transform player;
    private Vector3 targetPos;
    private bool isMoving = false;
    private float currentSpeed = 0f;
    private float animTimer = 0f;
    private bool animToggle = false;
    private Vector2 moveDir = Vector2.zero;
    private Vector2 lastDir = new Vector2(1, -1);
    private List<Vector3> waypoints = new List<Vector3>();
    private float waypointReachThreshold = 0.12f;

    // Patrulha
    private float waitTimer = 0f;

    // Combate
    private float shootTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        targetPos = transform.position;
        UpdateSprite(lastDir, false, true);
        ChooseNewPatrolPoint();
    }

    void Update()
    {
        shootTimer -= Time.deltaTime;

        if (player != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, player.position);

            // Se o jogador estiver dentro do campo de visão
            if (distToPlayer <= detectionRange)
            {
                // Mover até uma distância segura
                if (distToPlayer > attackRange)
                {
                    SetTarget(player.position);
                }
                else
                {
                    isMoving = false;
                    currentSpeed = 0f;
                    TryShootAtPlayer();
                }
            }
            else
            {
                HandlePatrol();
            }
        }
        else
        {
            HandlePatrol();
        }

        HandleMovement();
        UpdateAnimation();
    }

    void HandlePatrol()
    {
        if (!isMoving)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                ChooseNewPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    void ChooseNewPatrolPoint()
    {
        Vector2 randomPoint = (Vector2)transform.position + Random.insideUnitCircle * patrolRadius;

        if (IsOnGround(randomPoint) && !IsInWater(randomPoint))
        {
            SetTarget(randomPoint);
        }
    }

    // CORREÇÃO: Alterado de 'void' para 'public void'
    public void SetTarget(Vector3 pos)
    {
        if (!IsInWater(pos) && IsOnGround(pos))
        {
            waypoints.Clear();
            if (PathCrossesWater(transform.position, pos))
            {
                Vector3? bridgePoint = FindNearestBridge(transform.position);
                if (bridgePoint.HasValue)
                {
                    waypoints.Add(bridgePoint.Value);
                    waypoints.Add(pos);
                }
                else
                {
                    return;
                }
            }
            else
            {
                waypoints.Add(pos);
            }

            targetPos = waypoints[0];
            isMoving = true;
        }
    }

    void HandleMovement()
    {
        if (!isMoving || waypoints.Count == 0) return;

        Vector3 currentTarget = waypoints[0];
        Vector3 toTarget = currentTarget - transform.position;
        float dist = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;

        Vector3 nextPos = transform.position + dir * moveSpeed * Time.deltaTime;
        if (IsInWater(nextPos))
        {
            Vector3? bridgePoint = FindNearestBridge(transform.position);
            if (bridgePoint.HasValue)
            {
                waypoints.Insert(0, bridgePoint.Value);
                currentTarget = waypoints[0];
                dir = (currentTarget - transform.position).normalized;
            }
            else
            {
                isMoving = false;
                return;
            }
        }

        float desiredSpeed = moveSpeed;
        if (dist < 0.5f)
            desiredSpeed = Mathf.Lerp(0f, moveSpeed, dist / 0.5f);

        float accel = desiredSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * Time.deltaTime);

        moveDir = dir;
        transform.position += (Vector3)(moveDir * currentSpeed * Time.deltaTime);

        if (dist < Mathf.Max(stopDistance, waypointReachThreshold))
        {
            waypoints.RemoveAt(0);
            if (waypoints.Count > 0)
                targetPos = waypoints[0];
            else
                isMoving = false;
        }
    }

    void TryShootAtPlayer()
    {
        if (shootTimer <= 0f && player != null)
        {
            shootTimer = shootCooldown;

            if (bulletPrefab != null && firePoint != null)
            {
                Vector2 dir = (player.position - firePoint.position).normalized;
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

                // Configurar componente Bullet (marca como inimiga, define direção, dano e velocidade)
                Bullet b = bullet.GetComponent<Bullet>();
                if (b != null)
                {
                    b.SetDirection(dir);
                    b.isEnemyBullet = true;
                    b.damage = bulletDamage;
                    b.speed = bulletSpeed;
                }
                else
                {
                    // fallback: definir velocity diretamente se Bullet não existir
                    Rigidbody2D rbFallback = bullet.GetComponent<Rigidbody2D>();
                    if (rbFallback != null)
                        rbFallback.velocity = dir * bulletSpeed;
                }

                // Ignorar colisões entre a bala e o próprio inimigo (se ambos tiverem Collider2D)
                Collider2D bulletCol = bullet.GetComponent<Collider2D>();
                if (bulletCol != null)
                {
                    Collider2D[] ownCols = GetComponents<Collider2D>();
                    foreach (var c in ownCols)
                    {
                        if (c != null)
                            Physics2D.IgnoreCollision(bulletCol, c);
                    }
                }

                Destroy(bullet, 3f);
            }
        }
    }

    bool PathCrossesWater(Vector3 start, Vector3 end)
    {
        float totalDist = Vector2.Distance(start, end);
        if (totalDist < 0.01f) return false;

        float step = 0.12f;
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
                return true;
        }

        return false;
    }

    Vector3? FindNearestBridge(Vector3 from)
    {
        float searchRadius = 2f;
        while (searchRadius <= 20f)
        {
            var hits = Physics2D.OverlapCircleAll(from, searchRadius, bridgeLayer);
            if (hits.Length > 0)
            {
                var sorted = hits.OrderBy(h => Vector2.Distance(from, h.bounds.center));
                foreach (var h in sorted)
                {
                    Vector3 candidate = h.ClosestPoint(from);
                    if (!PathCrossesWater(from, candidate))
                        return candidate;
                }
            }
            searchRadius += 2f;
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
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        Sprite chosen = frontRight_Idle;

        if (angle >= 15f && angle < 75f)
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);
        else if (angle >= 75f && angle < 105f)
            chosen = idle ? backRight_Idle : (toggle ? backRight_L : backRight_R);
        else if (angle >= 105f && angle < 165f)
            chosen = idle ? backLeft_Idle : (toggle ? backLeft_L : backLeft_R);
        else if (angle >= 165f && angle < 255f)
            chosen = idle ? frontLeft_Idle : (toggle ? frontLeft_L : frontLeft_R);
        else if (angle >= 255f && angle < 315f)
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);
        else
            chosen = idle ? frontRight_Idle : (toggle ? frontRight_L : frontRight_R);

        sr.sprite = chosen;
    }

    bool IsInWater(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, waterLayer) != null;
    }

    bool IsOnGround(Vector3 position)
    {
        return Physics2D.OverlapCircle(position, 0.2f, groundLayer) != null ||
               Physics2D.OverlapCircle(position, 0.2f, bridgeLayer) != null;
    }
}