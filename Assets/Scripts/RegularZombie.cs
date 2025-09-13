using UnityEngine;
using UnityEngine.AI;

public class RegularZombie : EnemyBase
{
    [Header("Detection Settings")]
    [SerializeField] private float range = 8f;
    [SerializeField] private float hearRange = 4f;
    [SerializeField] private float fov = 90f;
    [SerializeField] private int rayCount = 5;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float hearMod = 3f;

    [Header("Idle Settings")]
    [SerializeField] private float idleMinTime = 2f;
    [SerializeField] private float idleMaxTime = 5f;
    [SerializeField] private float idleRadius = 3f;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 5f;

    [Header("Speed Settings")]
    [SerializeField] private float nightSpeedMod = 1.5f;
    [SerializeField] private float LOSSpeed = 3f;
    [SerializeField] private float idleSpeed = 2f;

    private float idleTimer;
    private float searchTimer;
    private Vector2 facingDir = Vector2.right;
    private Vector2? lastKnownPlayerPos;
    private bool isSearching;
    private bool didSee = false;


    public override void Tick()
    {
        if(didSee || isSearching)
        {
            if (isDay)
                agent.speed = LOSSpeed;
            else
                agent.speed = LOSSpeed + nightSpeedMod;
        }
        else
        {
            if (isDay)
                agent.speed = idleSpeed;
            else
                agent.speed = idleSpeed + nightSpeedMod;
        }
        if (DetectPlayer())
        {
            agent.SetDestination(player.position);
            isSearching = false;
        }
        else if (isSearching && lastKnownPlayerPos.HasValue)
        {
            HandleSearch();
        }
        else
        {
            HandleIdle();
        }

        if (agent.hasPath)
        {
            Vector2 moveDir = agent.desiredVelocity.normalized;
            if (moveDir != Vector2.zero)
                facingDir = moveDir;
        }
    }

    private bool DetectPlayer()
    {
        float dist = Vector2.Distance(player.position, transform.position);
        float mod = 0f;
        if (player.GetComponent<PlayerInterface>().GetCrouch())
        {
            mod = hearMod;
        }
        if (dist <= hearRange - mod)
        {
            didSee = true;
            return true;
        }
        if (dist <= range && FanVisionCheck())
        {

            didSee = true;
            return true;
        }

        if (didSee)
        {
            lastKnownPlayerPos = player.position;
            isSearching = true;
            searchTimer = searchDuration;
            didSee = false;
        }
        return false;
    }

    private bool FanVisionCheck()
    {
        float startAngle = -fov / 2f;
        float step = fov / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * facingDir;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, range, hitLayers);
            if (hit && hit.transform.TryGetComponent<PlayerInterface>(out _))
                return true;
        }
        return false;
    }

    private void HandleSearch()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
        {
            if (!agent.hasPath || agent.remainingDistance < 0.2f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * searchRadius;
                Vector3 targetPos = lastKnownPlayerPos.Value + randomOffset;
                agent.SetDestination(targetPos);
            }
        }
        else
        {
            isSearching = false;
            lastKnownPlayerPos = null;
        }
    }

    private void HandleIdle()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f) 
        {
            Vector2 randomOffset = Random.insideUnitCircle * idleRadius;
            Vector3 idleTarget = transform.position + (Vector3)randomOffset;
            agent.SetDestination(idleTarget);

            idleTimer = Random.Range(idleMinTime, idleMaxTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        float startAngle = -fov / 2f;
        float step = fov / (rayCount - 1);
        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * facingDir;
            Gizmos.DrawRay(transform.position, dir * range);
        }

        if (lastKnownPlayerPos.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lastKnownPlayerPos.Value, 0.2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastKnownPlayerPos.Value, searchRadius);
        }
    }
}
