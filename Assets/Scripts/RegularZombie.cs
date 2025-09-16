using UnityEngine;
using UnityEngine.AI;

public class RegularZombie : EnemyBase
{
    [Header("Detection Settings")]
    [SerializeField] private float activeRange = 100;
    [SerializeField] private float baseRange = 8f;
    [SerializeField] private float rangeNight = 4f;
    [SerializeField] private float baseHearRange = 4f;
    [SerializeField] private float baseFOV = 90f;
    [SerializeField] private float fovNight = 30f;
    [SerializeField] private int rayCount = 5;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float sneakHearing = 3f;
    [SerializeField] private float gunshotHearing = 100f;
    [SerializeField] private float hearNight = 1f;
    [SerializeField] private float hordeHearing = 15f;
    [SerializeField] private float hordeMentality = 30f;

    [Header("Idle Settings")]
    [SerializeField] private float idleMinTime = 2f;
    [SerializeField] private float idleMaxTime = 5f;
    [SerializeField] private float idleRadius = 3f;

    [Header("Search Settings")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 5f;

    [Header("Speed Settings")]
    [SerializeField] private float nightSpeedMod = 1.5f;
    [SerializeField] private float baseLOSSpeed = 3f;
    [SerializeField] private float baseIdleSpeed = 2f;

    [Header("Performace Settings")]
    [SerializeField] private int detectionIntervalFrames = 5;

    [Header("Animation Stuff")]
    [SerializeField] private GameObject upObj;
    [SerializeField] private GameObject downObj;
    [SerializeField] private GameObject leftObj;
    [SerializeField] private GameObject rightObj;
    [SerializeField] private GameObject mainSprite;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    //private RegularZombie hordeController;
    private int frameOffset;
    private float idleTimer;
    private float searchTimer;
    private Vector2 facingDir = Vector2.right;
    private Vector2? lastKnownPlayerPos;
    private bool isSearching;
    private bool didSee = false;
    private float LOSSpeed = 0;
    private float idleSpeed = 0;
    private float range = 0;
    private float fov = 0;
    private float hearRange = 0;
    private float distanceFromPlayer = 0;
    //private bool isController = false;

    private void Awake()
    {
        frameOffset = Random.Range(0, detectionIntervalFrames);
    }
    private bool ShouldDetectThisFrame()
    {
        return Time.frameCount % detectionIntervalFrames == frameOffset;
    }

    public override void Tick()
    {

        bool detected = false;
        if (isDay)
        {
            LOSSpeed = baseLOSSpeed;
            idleSpeed = baseIdleSpeed;
            range = baseRange;
            fov = baseFOV;
            hearRange = baseHearRange;
        }
        else
        {
            LOSSpeed = baseLOSSpeed + nightSpeedMod;
            idleSpeed = baseIdleSpeed + nightSpeedMod;
            range = rangeNight;
            fov = fovNight;
            hearRange = hearNight;
        }

        if (didSee || isSearching)
            agent.speed = LOSSpeed;
        else
            agent.speed = idleSpeed;

        if (ShouldDetectThisFrame())
        {
            distanceFromPlayer = Vector2.Distance(player.position, transform.position);
            if (distanceFromPlayer > activeRange) return;
            detected = DetectPlayer();
        }
        else
            return;

        if (player.GetComponent<PlayerInterface>().GetIsSeenAndChased())
        {
            if(distanceFromPlayer <= hordeHearing)
            {
                detected = true;
            }
        }

        if (detected)
        {
            didSee = true;
            agent.SetDestination(player.position);
            player.GetComponent<PlayerInterface>().SetIsSeenAndChased(true);
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

            UpdateAnimations(moveDir);
        }
        else
        {
            UpdateAnimations(Vector2.zero);
        }
    }
    private void UpdateAnimations(Vector2 moveDir)
    {
        // Hide all directional objects first
        upObj.SetActive(false);
        downObj.SetActive(false);
        leftObj.SetActive(false);
        rightObj.SetActive(false);

        if (moveDir.sqrMagnitude > 0.01f) // zombie is moving
        {
            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
            {
                if (moveDir.x > 0)
                    rightObj.SetActive(true);
                else
                    leftObj.SetActive(true);
            }
            else
            {
                if (moveDir.y > 0)
                    upObj.SetActive(true);
                else
                    downObj.SetActive(true);
            }
        }
        else // zombie stopped moving
        {
            // Use the last facingDir to pick idle sprite
            if (Mathf.Abs(facingDir.x) > Mathf.Abs(facingDir.y))
            {
                spriteRenderer.sprite = facingDir.x > 0 ? rightSprite : leftSprite;
            }
            else
            {
                spriteRenderer.sprite = facingDir.y > 0 ? upSprite : downSprite;
            }
        }
    }

    private bool DetectPlayer()
    {
        float mod = 0f;
        if (player.GetComponent<PlayerInterface>().GetCrouch())
        {
            mod = hearRange - sneakHearing;
        }
        if (player.GetComponent<PlayerInterface>().GetShottingBool())
        {
            mod = gunshotHearing + player.GetComponent<PlayerInterface>().GetActiveWeapon().soundMod;
        }
        if (distanceFromPlayer <= mod)
        {
            didSee = true;
            return true;
        }
        if (distanceFromPlayer <= range && FanVisionCheck())
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
        searchTimer -= Time.deltaTime * detectionIntervalFrames;

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
        idleTimer -= Time.deltaTime * detectionIntervalFrames;

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
