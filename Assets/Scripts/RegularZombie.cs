using UnityEngine;

public class RegularZombie : EnemyBase
{
    [Header("Detection")]
    [SerializeField] private float range = 10f;
    [SerializeField] private float hearRange = 5f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float fov = 90f;  
    [SerializeField] private int rayCount = 7;
    [SerializeField] private float crouchMod = 3;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector2 idleMoveTimeRange = new Vector2(1f, 3f);
    [SerializeField] private Vector2 idleWaitTimeRange = new Vector2(1f, 4f);

    [Header("Search Behavior")]
    [SerializeField] private float searchRadius = 3f;
    [SerializeField] private float searchTime = 5f;

    private Rigidbody2D rb;
    private Vector2 facingDir = Vector2.right;
    private Vector2 idleDirection;
    private float idleTimer = 0f;
    private bool isIdleMoving = false;

    private Vector2? lastKnownPlayerPos = null; 
    private float searchTimer = 0f;
    private bool isSearching = false;
    private Vector2 currentSearchTarget;
    private bool hasSearchTarget = false;

    private bool seesPlayer = false;
    private float dist = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Tick()
    {
        seesPlayer = DetectPlayer();

        if (seesPlayer)
        {
            lastKnownPlayerPos = player.position;
            isSearching = false;
            searchTimer = 0f;
            hasSearchTarget = false;
        }

        if (lastKnownPlayerPos.HasValue)
        {
            Vector2 target = lastKnownPlayerPos.Value;
            Vector2 toTarget = target - rb.position;

            if (toTarget.magnitude > 0.1f)
            {
                facingDir = toTarget.normalized;
                rb.MovePosition(rb.position + facingDir * moveSpeed * Time.deltaTime);
            }
            else
            {
                if (!isSearching)
                {
                    isSearching = true;
                    searchTimer = searchTime;
                }
                else
                {
                    HandleSearch();
                }
            }
        }
        else
        {
            HandleIdleWander();
        }
    }

    private bool DetectPlayer()
    {
        dist = Vector2.Distance(player.position, transform.position);

        float hearmod = 0;
        if (player.GetComponent<PlayerInterface>().GetCrouch())
        {
            hearmod = crouchMod;
        }

        if (dist <= hearRange + hearmod) return true;
        if (dist <= range && FanVisionCheck()) return true;
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
            if (hit.collider != null && hit.collider.TryGetComponent<PlayerInterface>(out var _))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleIdleWander()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            if (isIdleMoving)
            {
                idleTimer = Random.Range(idleWaitTimeRange.x, idleWaitTimeRange.y);
                isIdleMoving = false;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                idleTimer = Random.Range(idleMoveTimeRange.x, idleMoveTimeRange.y);
                isIdleMoving = true;
                idleDirection = Random.insideUnitCircle.normalized;
                facingDir = idleDirection; // update facing
            }
        }

        if (isIdleMoving)
        {
            rb.MovePosition(rb.position + idleDirection * moveSpeed * Time.deltaTime * 0.5f);
        }
    }

    private void HandleSearch()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
        {
            if (!hasSearchTarget || Vector2.Distance(rb.position, currentSearchTarget) < 0.2f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * searchRadius;
                currentSearchTarget = lastKnownPlayerPos.Value + randomOffset;
                hasSearchTarget = true;
            }

            Vector2 dir = (currentSearchTarget - rb.position).normalized;
            facingDir = dir;
            rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime * 0.5f);
        }
        else
        {
            isSearching = false;
            hasSearchTarget = false;
            lastKnownPlayerPos = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (Application.isPlaying)
        {
            float startAngle = -fov / 2f;
            float step = fov / (rayCount - 1);

            for (int i = 0; i < rayCount; i++)
            {
                float angle = startAngle + step * i;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * facingDir;
                Gizmos.DrawRay(transform.position, dir * range);
            }
        }
        else
        {
            Gizmos.DrawRay(transform.position, Vector2.right * range);
        }

        if (lastKnownPlayerPos.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lastKnownPlayerPos.Value, 0.2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastKnownPlayerPos.Value, searchRadius);

            if (hasSearchTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentSearchTarget, 0.15f);
            }
        }
    }
}