using UnityEngine;

public class RegularZombie : EnemyBase
{
    [SerializeField] private float range;
    [SerializeField] private float hearRange;
    [SerializeField] private LayerMask hitLayers;

    public override void Tick()
    {
        bool GoToPlayer = false;
        float dist = Vector2.Distance(player.position, gameObject.transform.position);
        if(player.GetComponent<PlayerInterface>().GetCrouch())
        {
            if (dist <= hearRange)
            {
                GoToPlayer = true;
            }
        }
        else
        {
            if (dist <= range)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, range, hitLayers);
                hit.transform.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface playerInterface);
                if (playerInterface != null)
                {
                    GoToPlayer = true;
                }
            }
        }

        if (GoToPlayer)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.SetDestination(player.position);
            agent.isStopped = true;
        }
    }
}
