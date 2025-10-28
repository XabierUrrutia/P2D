using UnityEngine;
[RequireComponent(typeof(CharacterMovement2D))]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyShooting))]
public class EnemyAI : MonoBehaviour
{
    public float detectionRange = 10f;
    public float attackStopDistance = 4f;
    public float moveInterval = 3f;
    public float moveRadius = 5f;
    public LayerMask groundLayer;
    public LayerMask waterLayer;

    private CharacterMovement2D movement;
    private EnemyShooting shooter;
    private Transform player;
    private Vector3 randomTarget;
    private float timer;
    private bool chasing = false;

    void Start()
    {
        movement = GetComponent<CharacterMovement2D>();
        shooter = GetComponent<EnemyShooting>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        randomTarget = transform.position;
        timer = moveInterval;
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // Detectar jogador
        if (distToPlayer <= detectionRange)
        {
            chasing = true;
        }
        else if (distToPlayer > detectionRange * 1.2f)
        {
            chasing = false;
        }

        if (chasing)
        {
            if (distToPlayer > attackStopDistance)
            {
                movement.SetTarget(player.position);
            }
            else
            {
                movement.SetTarget(transform.position); // parar para disparar
            }
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            randomTarget = GetRandomGroundPosition();
            movement.SetTarget(randomTarget);
            timer = moveInterval;
        }
    }

    Vector3 GetRandomGroundPosition()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = transform.position + (Vector3)(Random.insideUnitCircle * moveRadius);
            if (IsOnGround(randomPos) && !IsInWater(randomPos))
                return randomPos;
        }
        return transform.position;
    }

    bool IsOnGround(Vector3 position) => Physics2D.OverlapCircle(position, 0.2f, groundLayer);
    bool IsInWater(Vector3 position) => Physics2D.OverlapCircle(position, 0.2f, waterLayer);
}
