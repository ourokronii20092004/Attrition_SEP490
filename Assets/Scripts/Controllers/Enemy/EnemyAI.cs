using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("---- NETWORK MULTIPLAYER ----")]
    [Tooltip("Chỉ bật true nếu máy này là Host/Server để tránh giật lag 2 bên tranh nhau điều khiển quái")]
    public bool hasAuthority = true; // Sẽ được Networking điều khiển

    [Header("---- CONNECTIONS ----")]
    public EnemyController enemyCombat;
    public Animator anim;

    [Header("---- MOVEMENT ----")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("---- WALL DETECTION ----")]
    [SerializeField] private float wallCheckDistance = 0.7f;

    [Header("---- GIVE UP LOGIC ----")]
    [SerializeField] private float giveUpDuration = 3.0f;

    private bool isGivingUp = false;
    private float giveUpTimer = 0f;

    [Header("---- PATROL ----")]
    [SerializeField] private float patrolDistance = 2f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("---- VISION ----")]
    [SerializeField] private float viewRadius = 5f;
    [Range(0, 360)][SerializeField] private float viewAngle = 90f;

    private Rigidbody2D rb;
    private Transform closestPlayer;
    private Vector2 startPosition;
    private Vector2 currentTarget;

    private bool isChasing = false;
    private bool isReturning = false;
    private bool movingRight = true;
    private float waitTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyCombat == null) enemyCombat = GetComponent<EnemyController>();

        startPosition = transform.position;
        PickNextPatrolPoint();
    }

    private void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform currentClosest = null;

        foreach (GameObject p in players)
        {
            float distance = Vector2.Distance(transform.position, p.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentClosest = p.transform;
            }
        }
        closestPlayer = currentClosest;
    }

    void FixedUpdate()
    {
        // Nếu không phải là Server/Host, quái chỉ việc chạy Animation theo gói tin mạng gửi về, không tự tính AI.
        if (!hasAuthority) return;

        // Luôn cập nhật vị trí Player gần nhất để đuổi
        FindClosestPlayer();

        if (isGivingUp)
        {
            giveUpTimer -= Time.fixedDeltaTime;
            if (giveUpTimer <= 0)
            {
                isGivingUp = false;
                isChasing = false;
                isReturning = true;
                currentTarget = startPosition;
                if (movingRight) PickNextPatrolPoint();
            }
        }

        if (enemyCombat != null && enemyCombat.IsKnockbackActive)
        {
            UpdateAnimation();
            return;
        }

        if (enemyCombat != null && enemyCombat.IsDead())
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation();
            return;
        }

        if (enemyCombat != null && enemyCombat.IsAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation();
            return;
        }

        if (closestPlayer != null)
        {
            if (CanSeePlayer())
            {
                isGivingUp = false;
                isChasing = true;
                isReturning = false;
                currentTarget = closestPlayer.position;
            }
            else
            {
                if (isChasing && !isGivingUp) TriggerGiveUp();
            }
        }

        if (isChasing)
        {
            float attackRange = enemyCombat != null ? enemyCombat.attackRange : 0.8f;
            float distTotal = Vector2.Distance(transform.position, currentTarget);

            if (!isGivingUp && closestPlayer != null)
                currentTarget = closestPlayer.position;

            if (!isGivingUp && distTotal <= attackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                FaceTarget(currentTarget);
                if (enemyCombat != null) enemyCombat.AttemptAttack();
            }
            else
            {
                MoveToTarget(chaseSpeed);
            }
        }
        else if (isReturning)
        {
            float distToStart_X = Mathf.Abs(transform.position.x - startPosition.x);
            if (distToStart_X < 0.2f)
            {
                isReturning = false;
                PickNextPatrolPoint();
                waitTimer = waitTimeAtPoint;
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

        UpdateAnimation();
    }

    void UpdateAnimation() { if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); }

    void MoveToTarget(float currentSpeed)
    {
        float distanceX = Mathf.Abs(transform.position.x - currentTarget.x);

        if (isGivingUp && distanceX < 0.2f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (!isChasing && distanceX < 0.2f)
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
        bool hitWall = Physics2D.Raycast(origin, direction, wallCheckDistance, obstacleLayer);

        if (hitWall)
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

    void TriggerGiveUp()
    {
        isGivingUp = true;
        giveUpTimer = giveUpDuration;
    }

    void PatrolLogic()
    {
        float distanceX = Mathf.Abs(transform.position.x - currentTarget.x);
        if (distanceX < 0.2f)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0) { PickNextPatrolPoint(); waitTimer = waitTimeAtPoint; }
        }
    }

    void PickNextPatrolPoint()
    {
        if (movingRight) currentTarget = startPosition + Vector2.right * patrolDistance;
        else currentTarget = startPosition + Vector2.left * patrolDistance;
        movingRight = !movingRight;
    }

    void FaceTarget(Vector2 targetPos)
    {
        float dirX = (targetPos.x > transform.position.x) ? 1f : -1f;
        Vector3 scale = transform.localScale;
        if (dirX > 0) transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        else transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
        movingRight = (dirX > 0);
    }

    public void ForceFacePlayer()
    {
        FindClosestPlayer(); // Đảm bảo quay mặt đúng người vừa đánh mình (gần nhất)
        if (closestPlayer != null)
        {
            FaceTarget(closestPlayer.position);
            if (!isChasing)
            {
                isChasing = true;
                isReturning = false;
                isGivingUp = false;
                currentTarget = closestPlayer.position;
            }
            else if (isGivingUp)
            {
                isGivingUp = false;
                currentTarget = closestPlayer.position;
            }
        }
    }

    bool CanSeePlayer()
    {
        if (closestPlayer == null) return false;

        Vector2 dirToPlayer = (closestPlayer.position - transform.position).normalized;
        float dst = Vector2.Distance(transform.position, closestPlayer.position);

        if (dst > viewRadius) return false;

        Vector2 facing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        if (Vector2.Angle(facing, dirToPlayer) < viewAngle / 2f)
        {
            if (!Physics2D.Raycast(transform.position, dirToPlayer, dst, obstacleLayer))
            {
                return true;
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Vector3 origin = transform.position + new Vector3(0, -0.5f, 0);
        Gizmos.DrawLine(origin, origin + direction * wallCheckDistance);

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