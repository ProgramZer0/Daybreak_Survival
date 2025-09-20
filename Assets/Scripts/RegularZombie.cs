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
    [SerializeField] private float sprintHearing = 10f;
    [SerializeField] private float gunshotHearing = 100f;
    [SerializeField] private float hearNight = 1f;
    [SerializeField] private float dummmyLOSTime = 2f;

    [Header("Horde Settings")]
    [SerializeField] private float hordeAccuracy = 5f;
    [SerializeField] private float hordeHearing = 15f;
    [SerializeField] private float hordeMentality = 30f;
    [SerializeField] private float hordeCooldown = 10f;

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

    [Header("Stats Settings")]
    [SerializeField] private float stunTime = 1f;
    [SerializeField] private float stunChance = 1f;

    [Header("Sound settings")]
    [SerializeField] private float randomSoundInterval = 15f;
    [SerializeField] private Sound[] zombieSounds;
    [SerializeField] private Sound[] hurtSounds;

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
    private float hordeAssistTimer = 0f;
    private float nextSound = 0f;
    private GameObject activeDirectionObj;
    private float losTimer = 0f;
    private float stunTimer = 0f;
    private float soundTimer = 0f;
    private bool lostLOS = false;

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
        if (!ShouldDetectThisFrame())
            return;

        distanceFromPlayer = Vector2.Distance(player.position, transform.position);
        if (distanceFromPlayer > activeRange) return;

        if (hordeAssistTimer > 0f)
            hordeAssistTimer -= Time.deltaTime * detectionIntervalFrames;
        if(nextSound == 0)
            nextSound = Random.Range(1, randomSoundInterval);
        if(soundTimer >= nextSound)
        {
            nextSound = 0;
            PlaySound(zombieSounds[Random.Range(0, zombieSounds.Length)]);
        }
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


        if (isStunned)
        {
            stunTimer += Time.deltaTime * detectionIntervalFrames;
            if(stunTimer >= stunTime)
            {
                isStunned = false;
                stunTimer = 0;
            }
            else
                return;
        }


        if (DetectPlayer())
        {
            SM.PlayIfAlreadyNotPlaying("seePlayer");
            didSee = true;
            agent.SetDestination(player.position);
            playerInterface.SetIsSeenAndChased(true);
            isSearching = false;
        }
        else if(playerInterface.GetIsSeenAndChased())
        {

            if (distanceFromPlayer <= hordeHearing && hordeAssistTimer <= 0f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * hordeAccuracy;
                Vector2 playerGuessLocation = (Vector2)player.position + randomOffset;

                agent.SetDestination(playerGuessLocation);
                hordeAssistTimer = hordeCooldown;
            }
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
    public override void TakeDamage(float damage, bool _isStunned)
    {
        health -= damage;

        PlaySound(hurtSounds[Random.Range(0, hurtSounds.Length)]);

        if (health <= 0)
        {
            OnDeath();
        }
        if (agent != null && player != null)
            agent.SetDestination(player.position);
        float rand = Random.Range(0, 1);
        if (!_isStunned)
            return;

        if(rand <= stunChance)
        {
            isStunned = true;
            stunTimer = 0f;
        }
        else
        {
            isStunned = false;
            stunTimer = 0f;
        }
    }
    private void UpdateAnimations(Vector2 moveDir)
    {
        const float moveThreshold = 0.05f;

        if (moveDir.sqrMagnitude > moveThreshold)
        {
            mainSprite.SetActive(false);

            GameObject newActive;

            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
            {
                newActive = moveDir.x > 0 ? rightObj : leftObj;
            }
            else
            {
                newActive = moveDir.y > 0 ? upObj : downObj;
            }

            if (newActive != activeDirectionObj)
            {
                if (activeDirectionObj != null)
                    activeDirectionObj.SetActive(false);

                newActive.SetActive(true);
                activeDirectionObj = newActive;
            }

            facingDir = moveDir.normalized;
        }
        else
        {
            if (activeDirectionObj != null)
            {
                activeDirectionObj.SetActive(false);
                activeDirectionObj = null;
            }

            mainSprite.SetActive(true);

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
        if (playerInterface.GetCrouch())
            mod = hearRange - sneakHearing;
        else if (playerInterface.GetShottingBool())
            mod = hearRange + sprintHearing;
        if (playerInterface.GetShottingBool())
            mod = gunshotHearing + playerInterface.GetActiveWeapon().soundMod;

        if (distanceFromPlayer <= mod)
        {
            didSee = true;
            lostLOS = false;
            return true;
        }
        if (distanceFromPlayer <= range && FanVisionCheck())
        {

            didSee = true;
            lostLOS = false;
            return true;
        }

        if (didSee && !lostLOS)
        {
            lostLOS = true;
            losTimer = dummmyLOSTime;
            lastKnownPlayerPos = player.position;
        }

        if (lostLOS)
        {
            losTimer -= Time.deltaTime * detectionIntervalFrames;

            if (losTimer > 0f)
            {
                if (lastKnownPlayerPos.HasValue)
                    agent.SetDestination(lastKnownPlayerPos.Value);
                return true; 
            }
            else
            {
                isSearching = true;
                searchTimer = searchDuration;
                didSee = false;
                lostLOS = false;
            }
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
                float t = 1f - (searchTimer / searchDuration);
                float currentRadius = Mathf.Lerp(searchRadius, 1f, t);

                Vector2 toPlayer = ((Vector2)player.position - lastKnownPlayerPos.Value).normalized;
                Vector2 biasedCenter = lastKnownPlayerPos.Value + toPlayer * (currentRadius * 0.7f);

                Vector2 randomOffset = Random.insideUnitCircle * (currentRadius * 0.3f);
                Vector3 targetPos = biasedCenter + randomOffset;

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
