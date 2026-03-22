using Fusion;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public EnemyController enemyController;
    public Animator anim;

    [Header("---- MOVEMENT ----")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public LayerMask obstacleLayer;
    public float wallCheckDistance = 0.7f;

    [Header("---- PATROL & GIVE UP ----")]
    public float patrolDistance = 2f;
    public float waitTimeAtPoint = 1f;
    public float giveUpDuration = 3.0f;

    [Header("---- VISION ----")]
    public float viewRadius = 5f;
    [Range(0, 360)] public float viewAngle = 90f;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private Vector2 startPosition;
    private Vector2 currentTarget;
    private Vector3 originalScale;

    private bool isChasing = false;
    private bool isReturning = false;
    private bool movingRight = true;
    private bool isGivingUp = false;

    [Networked] private TickTimer waitTimer { get; set; }
    [Networked] private TickTimer giveUpTimer { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        startPosition = transform.position;
        if (anim == null) anim = GetComponentInChildren<Animator>();

        PickNextPatrolPoint();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // 1. Ưu tiên Vật lý và Chết
        if (enemyController.IsDead || enemyController.IsKnockbackActive)
        {
            UpdateAnimation(0);
            return;
        }

        // 2. Ưu tiên Đòn đánh
        if (enemyController.IsAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(0);
            return;
        }

        // 3. Logic Bỏ cuộc
        if (isGivingUp && giveUpTimer.Expired(Runner))
        {
            isGivingUp = false;
            isChasing = false;
            isReturning = true;
            currentTarget = startPosition;
            if (movingRight) PickNextPatrolPoint();
        }

        // 4. Logic Tầm nhìn
        FindVisiblePlayer();

        if (playerTransform != null)
        {
            isGivingUp = false;
            isChasing = true;
            isReturning = false;
            currentTarget = playerTransform.position;
        }
        else if (isChasing && !isGivingUp)
        {
            TriggerGiveUp();
        }

        // 5. Logic Di chuyển
        if (isChasing)
        {
            float dist = Vector2.Distance(transform.position, currentTarget);
            if (!isGivingUp && dist <= enemyController.attackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                FaceTarget(currentTarget);
                enemyController.AttemptAttack();
            }
            else
            {
                MoveToTarget(chaseSpeed);
            }
        }
        else if (isReturning)
        {
            if (Mathf.Abs(transform.position.x - startPosition.x) < 0.2f)
            {
                isReturning = false;
                PickNextPatrolPoint();
                waitTimer = TickTimer.CreateFromSeconds(Runner, waitTimeAtPoint);
            }
            else
            {
                currentTarget = new Vector2(startPosition.x, transform.position.y);
                MoveToTarget(patrolSpeed);
            }
        }
        else
        {
            PatrolLogic();
            MoveToTarget(patrolSpeed);
        }

        UpdateAnimation(Mathf.Abs(rb.linearVelocity.x));
    }

    void FindVisiblePlayer()
    {
        playerTransform = null;
        float minDst = float.MaxValue;

        foreach (var player in Runner.ActivePlayers)
        {
            NetworkObject pObj = Runner.GetPlayerObject(player);
            if (pObj != null)
            {
                PlayerController pController = pObj.GetComponent<PlayerController>();
                if (pController != null && !pController.IsDead)
                {
                    Vector2 dirToPlayer = (pObj.transform.position - transform.position).normalized;
                    float dst = Vector2.Distance(transform.position, pObj.transform.position);

                    if (dst <= viewRadius)
                    {
                        Vector2 facing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                        if (Vector2.Angle(facing, dirToPlayer) < viewAngle / 2f)
                        {
                            if (!Physics2D.Raycast(transform.position, dirToPlayer, dst, obstacleLayer))
                            {
                                if (dst < minDst)
                                {
                                    minDst = dst;
                                    playerTransform = pObj.transform;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void MoveToTarget(float currentSpeed)
    {
        float distanceX = Mathf.Abs(transform.position.x - currentTarget.x);

        if ((isGivingUp || !isChasing) && distanceX < 0.2f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dirX = (currentTarget.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(dirX * currentSpeed, rb.linearVelocity.y);

        CheckWall(dirX);
        FaceTarget(currentTarget);
    }

    void CheckWall(float dirX)
    {
        Vector2 direction = dirX > 0 ? Vector2.right : Vector2.left;
        Vector2 origin = new Vector2(transform.position.x, transform.position.y - 0.5f);

        if (Physics2D.Raycast(origin, direction, wallCheckDistance, obstacleLayer))
        {
            if (isChasing)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (!isGivingUp) TriggerGiveUp();
            }
            else
            {
                PickNextPatrolPoint();
            }
        }
    }

    void PatrolLogic()
    {
        if (Mathf.Abs(transform.position.x - currentTarget.x) < 0.2f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (waitTimer.ExpiredOrNotRunning(Runner))
            {
                PickNextPatrolPoint();
                waitTimer = TickTimer.CreateFromSeconds(Runner, waitTimeAtPoint);
            }
        }
    }

    void PickNextPatrolPoint()
    {
        currentTarget = startPosition + (movingRight ? Vector2.right : Vector2.left) * patrolDistance;
        movingRight = !movingRight;
    }

    void FaceTarget(Vector2 targetPos)
    {
        float dirX = (targetPos.x > transform.position.x) ? 1f : -1f;
        float sign = dirX > 0 ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * sign, originalScale.y, originalScale.z);
        movingRight = (dirX > 0);
    }

    public void ForceFacePlayer()
    {
        if (playerTransform != null)
        {
            FaceTarget(playerTransform.position);
            if (!isChasing)
            {
                isChasing = true;
                isReturning = false;
                isGivingUp = false;
                currentTarget = playerTransform.position;
            }
            else if (isGivingUp)
            {
                isGivingUp = false;
                currentTarget = playerTransform.position;
            }
        }
    }

    void TriggerGiveUp()
    {
        isGivingUp = true;
        giveUpTimer = TickTimer.CreateFromSeconds(Runner, giveUpDuration);
    }

    void UpdateAnimation(float speedValue) { if (anim != null) anim.SetFloat("Speed", speedValue); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 facingDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Vector3 upperCone = Quaternion.Euler(0, 0, viewAngle / 2) * facingDir;
        Vector3 lowerCone = Quaternion.Euler(0, 0, -viewAngle / 2) * facingDir;
        Gizmos.DrawRay(transform.position, upperCone * viewRadius);
        Gizmos.DrawRay(transform.position, lowerCone * viewRadius);
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = Color.blue;
        Vector2 c2 = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawLine(c2 + Vector2.left * patrolDistance, c2 + Vector2.right * patrolDistance);
    }
}