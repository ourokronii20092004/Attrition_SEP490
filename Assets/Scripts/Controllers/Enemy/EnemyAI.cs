using Fusion;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public EnemyController enemyController;
    public float speed = 2f;
    private Transform targetPlayer;

    public override void FixedUpdateNetwork()
    {
        
        if (!HasStateAuthority || enemyController.IsDead) return;

        FindClosestPlayer();

        if (targetPlayer != null)
        {
            Vector2 moveDir = (targetPlayer.position - transform.position).normalized;
            GetComponent<Rigidbody2D>().linearVelocity = new Vector2(moveDir.x * speed, GetComponent<Rigidbody2D>().linearVelocity.y);

            
            transform.localScale = new Vector3(moveDir.x > 0 ? 1 : -1, 1, 1);
        }
    }

    void FindClosestPlayer()
    {
        float minDst = float.MaxValue;
        foreach (var player in Runner.ActivePlayers)
        {
            NetworkObject pObj = Runner.GetPlayerObject(player);
            if (pObj != null)
            {
                float dst = Vector2.Distance(transform.position, pObj.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    targetPlayer = pObj.transform;
                }
            }
        }
    }
}