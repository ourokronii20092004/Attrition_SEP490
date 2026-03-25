using Fusion;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    [Header("---- REFS ----")]
    [SerializeField] private EnemyAnimation animationComp;
    [SerializeField] private EnemyCombat combatComp;
    [SerializeField] private AxeDemonController controller;
    private Rigidbody2D rb;

    [Header("---- SETTINGS ----")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float viewRadius = 5f;
    public LayerMask obstacleLayer;

    private Vector2 startPosition;
    private Vector2 currentTarget;
    private Transform playerTarget;
    private bool isChasing = false;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        currentTarget = startPosition + Vector2.right * 2f;
    }

    public void RunAILogic()
    {
        if (combatComp.IsAttacking || controller.IsKnockbackActive)
        {
            rb.linearVelocity = Vector2.zero;
            animationComp.UpdateSpeed(0);
            return;
        }

        FindPlayer();

        if (isChasing && playerTarget != null)
        {
            currentTarget = playerTarget.position;
            float dist = Vector2.Distance(transform.position, currentTarget);

            if (dist <= combatComp.attackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                animationComp.FaceTarget(currentTarget.x);
                combatComp.AttemptAttack();
            }
            else
            {
                MoveTowards(currentTarget, chaseSpeed);
            }
        }
        else
        {
            Patrol();
        }

        animationComp.UpdateSpeed(Mathf.Abs(rb.linearVelocity.x));
    }

    private void FindPlayer()
    {
        playerTarget = null;
        isChasing = false;

        foreach (var player in Runner.ActivePlayers)
        {
            NetworkObject pObj = Runner.GetPlayerObject(player);
            if (pObj != null)
            {
                PlayerController pController = pObj.GetComponent<PlayerController>();
                if (pController != null && !pController.IsDead)
                {
                    float dst = Vector2.Distance(transform.position, pObj.transform.position);
                    if (dst <= viewRadius)
                    {
                        playerTarget = pObj.transform;
                        isChasing = true;
                        break;
                    }
                }
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.blue;
        Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawLine(center + Vector2.left * 2f, center + Vector2.right * 2f);
        Gizmos.DrawWireCube(center + Vector2.left * 2f, new Vector3(0.2f, 0.2f, 0));
        Gizmos.DrawWireCube(center + Vector2.right * 2f, new Vector3(0.2f, 0.2f, 0));
    }
    private void Patrol()
    {
        if (Mathf.Abs(transform.position.x - currentTarget.x) < 0.2f)
        {
            currentTarget = startPosition + (currentTarget.x > startPosition.x ? Vector2.left : Vector2.right) * 2f;
        }
        MoveTowards(currentTarget, patrolSpeed);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        float dirX = (target.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(dirX * speed, rb.linearVelocity.y);
        animationComp.FaceTarget(target.x);
    }

    public void ForceFacePlayer()
    {
        if (playerTarget != null) animationComp.FaceTarget(playerTarget.position.x);
    }
}