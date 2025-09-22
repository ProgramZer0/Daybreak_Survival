using System.Collections;
using UnityEngine;

public enum Direction
{
    N,
    NE,
    E,
    SE,
    S,
    SW,
    W,
    NW
}

public class PlayerInterface : MonoBehaviour
{

    [SerializeField] private MainGUIController GUI;
    [SerializeField] private SoundManager SM;
    [SerializeField] private GameManager GM;
    [SerializeField] private HPBar HpBar;
    [SerializeField] private WeaponHUD weaponHUD;
    [SerializeField] private CameraController CC;
    [SerializeField] private LayerMask dashMask;

    [SerializeField] private float normSpeed = 3f;
    [SerializeField] private float sprintSpeed = 4f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float dashCooldown = 10f;
    [SerializeField] private float dashRange = 3f;
    [SerializeField] private float dashOffset = 1f;
    [SerializeField] private float sprintTime = 10f;
    [SerializeField] private float sprintCooldown = 2f;
    [SerializeField] private float maxSprintDebuffTime = 5f;
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private float pickupRange = 1f;
    [SerializeField] private float maxHP = 10f;
    [SerializeField] private float hordeForgetTime = 5f;

    [SerializeField] private LayerMask interactLayer;
    public LayerMask EnemyLayer;
    [SerializeField] private Animator Animator;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject[] gunPoss;
    [SerializeField] private GameObject[] shootingLights;
    [SerializeField] private GameObject slashPrefab;

    [SerializeField] private string[] meleeSounds;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject dashIndObj;
    [SerializeField] private GameObject dashObj;

    [SerializeField] private GameObject pickupObj;

    [SerializeField] private Weapon currentWeapon;

    [SerializeField] private int weaponAmmo = 0;
    [SerializeField] private int maxAmmo = 0;

    [SerializeField] private float baseAnimationRange = 1f;
    [SerializeField] private float baseAnimationFOV = 70f;
    [SerializeField] private float meleeAnimationRangeOffset = 0.3f;
    
    private Rigidbody2D body;
    private Direction facing = Direction.S;
    private bool attacking = false;
    private bool meleeing = false;
    private bool interact = false;
    private bool dashing = false;
    private bool crouch = false;
    private bool sprinting = false;
    private bool pickupKey = false;
    private bool canShoot = true;
    private bool canMelee = true;
    private bool canDash = true;
    private bool IframesDown = true;
    private bool cannotMove = true;
    private bool crouchToggle = true;
    private bool isShooting = false;
    private bool canSprint = true;
    private bool wentOverSprint = false;
    private bool isSeenAndChased = false;
    private bool stepActive = false;
    private float inputH = 0f;
    private float inputV = 0f;
    private float currentHP;
    private float pickupTimer = 0;
    private float chaseTimer = 0;
    private float sprintTimer = 0;
    private pickup currentPick;

    public float ModNormSpeed = 0f;
    public float ModSprintSpeed = 0f;
    public float ModCrouchSpeed = 0f;
    public float ModDashCooldown = 0f;
    public float ModDashRange = 0f;
    public float ModDashOffset = 0f;
    public float ModSprintTime = 0f;
    public float ModSprintCooldown = 0f;
    public float ModMaxSprintDebuffTime = 0f;
    public float ModInteractRange = 0f;
    public float ModPickupRange = 0f;
    public float ModHordeForgetTime = 0f;

    public float ModMaxHP = 0f;
    public float ModSeeDistance = 0f;
    public float ModlightDistance = 0f;
    public bool hasNightVison = false;

    public void ChangedModValues()
    {
        maxHP = (maxHP + ModMaxHP);
    }
    public void ResetModValues()
    {
        ModNormSpeed = 0f;
        ModSprintSpeed = 0f;
        ModCrouchSpeed = 0f;
        ModDashCooldown = 0f;
        ModDashRange = 0f;
        ModDashOffset = 0f;
        ModSprintTime = 0f;
        ModSprintCooldown = 0f;
        ModMaxSprintDebuffTime = 0f;
        ModInteractRange = 0f;
        ModPickupRange = 0f;
        ModHordeForgetTime = 0f;
        ModMaxHP = 0f;
        ModSeeDistance = 0f;
        ModlightDistance = 0f;
        hasNightVison = false;
    }

    public float GetMaxHP() { return(maxHP + ModMaxHP); }
    public float GetCurrentHP() { return currentHP; }
    public float GetCooldown() { return (dashCooldown+ ModDashCooldown); }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ResetPlayerData();
    }

    public void ResetPlayerData()
    {
        body.linearVelocity = Vector2.zero;
        currentHP = (maxHP + ModMaxHP);
        weaponAmmo = 0;
        maxAmmo = 0;
        inputH = 0f;
        inputV = 0f;
        pickupTimer = 0;
        chaseTimer = 0;
        sprintTimer = 0;
        currentWeapon = GUI.ReturnEmptyWeapon();
        facing = Direction.S;
        attacking = false;
        meleeing = false;
        interact = false;
        dashing = false;
        crouch = false;
        sprinting = false;
        pickupKey = false;
        canShoot = true;
        canMelee = true;
        canDash = true;
        IframesDown = true;
        isShooting = false;
        canSprint = true;
        isSeenAndChased = false;
        stepActive = false;
        ResetModValues();
}

    private void Update()
    {
        if (cannotMove)
            return;

        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Mouse0)) attacking = true;
        if(weaponAmmo > 0)
            if (Input.GetKeyUp(KeyCode.Mouse0)) attacking = false;

        if (Input.GetKeyDown(KeyCode.Mouse1)) meleeing = true;

        if (Input.GetKeyDown(KeyCode.E)) interact = true;

        if (Input.GetKeyDown(KeyCode.F)) pickupKey = true;
        if (Input.GetKeyUp(KeyCode.F)) pickupKey = false;



        if (crouchToggle)
        {
            if (Input.GetKeyDown(KeyCode.C)) crouch = !crouch;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.C)) crouch = true;
            if (Input.GetKeyUp(KeyCode.C)) crouch = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (canSprint && sprintTimer < (sprintTime + ModSprintTime))
                sprinting = true;
        }
        
        if (Input.GetKeyUp(KeyCode.LeftShift)) 
        {
            if(sprintTimer < (sprintTime + ModSprintTime))
            {
                canSprint = false;
                sprinting = false;
                StartCoroutine(SprintingCooldown());
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) dashing = true;

        if (isSeenAndChased && chaseTimer == 0)
        {
            //SM.PlayMusic("chaseMusic", 0.3f);
            chaseTimer += Time.deltaTime;
        }
        if (chaseTimer >= (hordeForgetTime + ModHordeForgetTime))
        {
            isSeenAndChased = false;
            chaseTimer = 0;
            //GM.SetEnvMusic(10, true);
        }

        if (sprinting)
        {
            sprintTimer += Time.deltaTime;
        }
        else if (canSprint && sprintTimer > 0)
        {
            sprintTimer -= Time.deltaTime;
        }

        if (sprintTimer >= (sprintTime + ModSprintTime) && canSprint && !wentOverSprint)
        {
            canSprint = false;
            sprinting = false;
            wentOverSprint = true;
            sprintTimer += (maxSprintDebuffTime + ModMaxSprintDebuffTime);
            StartCoroutine(SprintingCooldown());
        }

        if (sprintTimer < (sprintTime + ModSprintTime))
            wentOverSprint = false;
    }
    
    public Weapon GetActiveWeapon()
    {
        return currentWeapon;
    }

    public bool GetSprinting() { return sprinting; }

    private void FixedUpdate()
    {
        if(currentHP < 0)
        {
            GM.EndGameFail();
        }

        if (cannotMove) 
        {
            animator.SetBool("isSneaking", false);
            animator.SetBool("isMoving", false);
            SM.Stop("footStep");
            return;
        }

        if (currentPick != null)
            TryPickUp();
        else
            GUI.StopSeeingPickup();

        Vector2 moveAmount = new Vector2(inputH, inputV).normalized;
        if (meleeing && canMelee)
        {
            TryMelee();
        }

        if (attacking && canShoot && !meleeing)
        {
            TryAttacking();
        }
        
        meleeing = false;

        if (interact)
        {
            SM.PlayIfAlreadyNotPlaying("Interact");
            TryInteract();
        }

        Move(moveAmount, dashing, sprinting);
    }

    private void TryPickUp()
    {
        if (Vector2.Distance(currentPick.gameObject.transform.position, transform.position) <= (pickupRange + ModPickupRange))
        {
            if (pickupKey)
            {
                SM.PlayIfAlreadyNotPlaying("pickingUpSound");
                pickupTimer += Time.deltaTime;
                if (!pickupObj.activeSelf)
                {
                    pickupObj.SetActive(true);
                    GUI.SeePickup(currentPick.weapon.pickupTime);
                    pickupObj.GetComponent<PickupIconScript>().triggerPickup();
                }

                if (pickupTimer >= currentPick.weapon.pickupTime)
                {
                    SM.Stop("pickingUpSound");
                    currentPick.addPickup(gameObject);
                    GUI.StopSeeingPickup();
                    pickupTimer = 0;
                }
            }
            else
            {
                SM.Stop("pickingUpSound");
                GUI.StopSeeingPickup();
                pickupTimer = 0;
            }
        }
        else
        {
            SM.Stop("pickingUpSound");
            currentPick = null;
            pickupTimer = 0;
            GUI.StopSeeingPickup();
        }
    }

    private void TryAttacking()
    {
        isShooting = false;
        if (GUI.GetInUI())
            return;
        if (weaponAmmo <= 0)
        {
            SM.Play("emptyAmmo");
            currentWeapon = GUI.ReturnEmptyWeapon();
            attacking = false;
            return;
        }

        int facingside = 0;

        switch(facing)
        {
            case Direction.N:  facingside = 0; break;
            case Direction.NW: facingside = 1; break;
            case Direction.W: facingside = 2; break;
            case Direction.SW: facingside = 3; break;
            case Direction.S: facingside = 4; break;
            case Direction.SE: facingside = 5; break;
            case Direction.E: facingside = 6; break;
            case Direction.NE: facingside = 7; break;
        }
        isShooting = true;

        for (int i = 0; i < currentWeapon.projectileAmount; i++)
        {
            Vector3 gunDirection = gunPoss[facingside].transform.position;
            Vector2 randomOffset = Random.insideUnitCircle * currentWeapon.spawnSpread;

            Vector2 gunDir = (gunPoss[facingside].transform.position - transform.position).normalized;
            Vector2 offset = gunDir * currentWeapon.offsetSpawnProjectile;

            Vector2 playerPos = (Vector2)transform.position + offset;
            Vector2 spawnPos = playerPos + randomOffset;

            GameObject o = GameObject.Instantiate(currentWeapon.projectilePrefab, spawnPos, Quaternion.Euler(0, 0, (facingside * 45)));

            Vector2 baseDir = (gunDirection - transform.position).normalized;
            float spread = currentWeapon.projectileSpread; // e.g. 45
            float randomAngle = Random.Range(-spread / 2f, spread / 2f);
            Vector2 moveDirection = RandomMovement(baseDir, randomAngle).normalized;

            o.GetComponent<Projectile>().SetValues(currentWeapon.projectileFallOffMultiplier, currentWeapon.projectileTime, 
                currentWeapon.splashRange, currentWeapon.splashDamage, currentWeapon.projectileDamage, 
                currentWeapon.projectileFallOffMultiplierTime, currentWeapon.projectileHasAnimation, currentWeapon.appearTime, currentWeapon.fadeInTime);
            
            o.GetComponent<Rigidbody2D>().linearVelocity = moveDirection * currentWeapon.projectileSpeed;  

        }

        SM.Play(currentWeapon.projectileSoundName);

        if (currentWeapon.hasFlash)
            StartCoroutine(EnableShootingLights(facingside));

        canShoot = false;

        weaponAmmo--;
        StartCoroutine(ShootingCoooldown(currentWeapon.projectileCooldown));
    }

    private void TryMelee()
    {
        if (GUI.GetInUI())
            return;

        if (!currentWeapon.canMelee && currentWeapon != null)
            return;
        
        SM.PlayRandomSound(meleeSounds);

        int facingside = 0;

        switch (facing)
        {
            case Direction.N: facingside = 0; break;
            case Direction.NW: facingside = 1; break;
            case Direction.W: facingside = 2; break;
            case Direction.SW: facingside = 3; break;
            case Direction.S: facingside = 4; break;
            case Direction.SE: facingside = 5; break;
            case Direction.E: facingside = 6; break;
            case Direction.NE: facingside = 7; break;
        }
        Vector2 dir = (gunPoss[facingside].transform.position - transform.position).normalized;

        float offset = currentWeapon.meleeRange * meleeAnimationRangeOffset; 
        Vector3 spawnPos = transform.position + (Vector3)dir * offset;

        MeleeAnimation(facingside, spawnPos);

        IEnemy en = FanCheck(dir);
        if(en != null)
        {
            float damage;

            if (currentWeapon == null)
                damage = 1f;
            else
                damage = currentWeapon.meleeDamage;

            en.TakeDamage(damage, currentWeapon);
        }

        canMelee = false;
        StartCoroutine(MeleeCoooldown(currentWeapon.meleeCooldown));
    }

    private void MeleeAnimation(int facingInt, Vector2 spawnPos)
    {
        if (slashPrefab != null)
        {
            GameObject slash = Instantiate(
                slashPrefab,
                spawnPos,
                Quaternion.Euler(0, 0, facingInt * 45f),
                transform
            );

            float fovScale = currentWeapon.meleeFOV / baseAnimationFOV;
            float scaleFactor = currentWeapon.meleeRange / baseAnimationRange;

            slash.transform.localScale = new Vector3(fovScale, scaleFactor, 1f);

            Destroy(slash, 0.4f);
        }   
    }

    private IEnemy FanCheck(Vector2 facingDir)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            currentWeapon.meleeRange,
            EnemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IEnemy>(out IEnemy enemy))
            {
                Vector2 toTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector2.Angle(facingDir, toTarget);

                if (angle <= currentWeapon.meleeFOV / 2f)
                {
                    Debug.DrawLine(transform.position, hit.transform.position, Color.green, 0.2f);
                    return enemy; 
                }
                else
                {
                    Debug.DrawLine(transform.position, hit.transform.position, Color.yellow, 0.2f);
                }
            }
        }

        return null;
    }

    private Vector2 RandomMovement(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
    private IEnumerator EnableShootingLights(int i)
    {
        shootingLights[i].SetActive(true);
        yield return new WaitForSeconds(.007f);
        shootingLights[i].SetActive(false);
    }
    private IEnumerator SprintingCooldown()
    {
        yield return new WaitForSeconds((sprintCooldown + ModSprintCooldown));
        canSprint = true;
    }
    private IEnumerator ShootingCoooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canShoot = true;
        isShooting = false;
    }
    private IEnumerator MeleeCoooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canMelee = true;
    }

    public void LockMovement(bool val)
    {
        cannotMove = val;
    }   

    private void TryInteract()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, (interactRange + ModInteractRange), interactLayer);
        if (hit != null)
        {
            hit.TryGetComponent<IInteractable>(out IInteractable interactable);
            if (interactable != null)
            {
                //Debug.Log("interacting");
                interactable.Interact(gameObject);
            }
        }
        interact = false;
    }

    private void Move(Vector2 move, bool dashed, bool isSprinting)
    {
        bool moving = true;
        float speeds = (normSpeed + ModNormSpeed);
        if (crouch)
        {
            speeds = (crouchSpeed+ ModCrouchSpeed);
            animator.speed = Mathf.Max(0.5f, (crouchSpeed+ ModCrouchSpeed) / (normSpeed + ModNormSpeed));
        }
        else if (isSprinting && canSprint)
        {
            speeds = (sprintSpeed+ ModSprintSpeed);
            animator.speed = (sprintSpeed+ ModSprintSpeed) / (normSpeed + ModNormSpeed);
        }
        else
            animator.speed = 1;

        body.linearVelocity = move * speeds;

        int facingInt = 0;
        if (inputH > 0)
        {
            if (inputV > 0)
            {
                facing = Direction.NE;
            }
            else if (inputV < 0)
            {
                facing = Direction.SE;
            }
            else
            {
                facing = Direction.E;
            }
        }
        else if (inputH < 0)
        {
            if (inputV > 0)
            {
                facing = Direction.NW;
            }
            else if (inputV < 0)
            {
                facing = Direction.SW;
            }
            else
            {
                facing = Direction.W;
            }
        }
        else
        {
            if (inputV > 0)
            {
                facing = Direction.N;
            }
            else if (inputV < 0)
            {
                facing = Direction.S;
            }
            else
            {
                moving = false;
            }
        }

        animator.SetBool("isSneaking", crouch);
        animator.SetBool("isMoving", moving);

        switch (facing)
        {
            case (Direction.N):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", true);
                animator.SetBool("FacingDown", false);
                facingInt = 0;
                break;
            case (Direction.NW):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", true);
                animator.SetBool("FacingDown", false);
                facingInt = 1;
                break;
            case (Direction.W):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", true);
                animator.SetBool("FacingUp", false);
                animator.SetBool("FacingDown", false);
                facingInt = 2;
                break;
            case (Direction.SW):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", false);
                animator.SetBool("FacingDown", true);
                facingInt = 3;
                break;
            case (Direction.S):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", false);
                animator.SetBool("FacingDown", true);
                facingInt = 4;
                break;
            case (Direction.SE):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", false);
                animator.SetBool("FacingDown", true);
                facingInt = 5;
                break;
            case (Direction.E):
                animator.SetBool("FacingRight", true);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", false);
                animator.SetBool("FacingDown", false);  
                facingInt = 6;
                break;
            case (Direction.NE):
                animator.SetBool("FacingRight", false);
                animator.SetBool("FacingLeft", false);
                animator.SetBool("FacingUp", true);
                animator.SetBool("FacingDown", false);
                facingInt = 7;
                break;
        }

        DisableCrossEnablex(facingInt);

        if(!moving)
        {
            dashing = false;
            return;
        }

        float speedTime = 0.3f;
        if (moving && crouch)
            speedTime = .6f;

        if (moving && isSprinting)
            speedTime = 0.2f;

        if (moving && !isSprinting && !crouch)
            speedTime = 0.3f;

        if (!stepActive && moving)
            StartCoroutine(PlayStep(speedTime));

        if (dashed && canDash && !crouch)
        {
            LockMovement(true);
            SM.Play("Dash");

            float playerSize = 0.2f;
            Vector2 dir = ((Vector2)gunPoss[facingInt].transform.position - (Vector2)transform.position ).normalized;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, playerSize, dir, (dashRange+ ModDashRange), dashMask);
            Vector2 dashTarget;

            if (hit)
                dashTarget = hit.point - dir * dashOffset;
            else
                dashTarget = transform.position + (Vector3)(dir * (dashRange+ ModDashRange));

            transform.position = dashTarget;

            animator.SetBool("isDashing", true);
            StartCoroutine(DashAnimationCooldown());

            GameObject o = GameObject.Instantiate(dashObj, transform.position, Quaternion.Euler(0, 0, (facingInt * 45)));
            canDash = false;
            dashIndObj.SetActive(true);
            dashIndObj.GetComponent<DashIndicator>().SetCooldown((dashCooldown+ ModDashCooldown));
            dashIndObj.GetComponent<DashIndicator>().TriggerCooldown();
            StartCoroutine(WaitDash());
        }
        dashing = false;
    }

    private IEnumerator PlayStep(float wait)
    {
        stepActive = true;
        SM.PlayrRandomPitch("footStep", .15f);
        yield return new WaitForSeconds(wait);
        stepActive = false;
    }
    public string GetAmmo()
    {
        return  weaponAmmo.ToString() + "/" + maxAmmo.ToString();
    }

    public bool GetIsSeenAndChased() { return isSeenAndChased; }
    public void SetIsSeenAndChased(bool s) { isSeenAndChased = s; }

    public bool GetShottingBool() 
    {
        if (weaponAmmo > 0)
            return isShooting;
        else
            return false;
    }

    private void DisableCrossEnablex(int i)
    {
        gunPoss[0].SetActive(false);
        gunPoss[1].SetActive(false);
        gunPoss[2].SetActive(false);
        gunPoss[3].SetActive(false);
        gunPoss[4].SetActive(false);
        gunPoss[5].SetActive(false);
        gunPoss[6].SetActive(false);
        gunPoss[7].SetActive(false);
        gunPoss[i].SetActive(true);
    }

    //why bool?
    public bool AddWeapon(Weapon weapon, int ammo)
    {
        
        //Debug.Log("adding weapon");
        if(currentWeapon != null && currentWeapon != GUI.ReturnEmptyWeapon())
        {
            //Debug.Log("spawning weapon");
            GameObject o = GameObject.Instantiate(currentWeapon.prefab, transform.position, Quaternion.identity);
            o.GetComponent<pickup>().currentAmmo = weaponAmmo;
        }

        currentWeapon = weapon;
        weaponAmmo = ammo;
        maxAmmo = weapon.maxAmmo;
        weaponHUD.SetCurrentWeapon(currentWeapon);
        CC.maxScrollOut = weapon.weaponZoom;
        CC.minScrollOut = weapon.weaponMinZoom;
        SM.Play(currentWeapon.equipSoundName);
        return true;
    }
    public bool AddAmmo(WeaponAmmoType type, int amount)
    {
        SM.Play("ammoPickup");
        if (type != currentWeapon.weaponAmmoType)
            return false;
        if (weaponAmmo == maxAmmo)
            return false;
        else
        {
            weaponAmmo += amount;
            if (weaponAmmo > maxAmmo)
                weaponAmmo = maxAmmo;
            return true;
        }
    }

    public void SetCrouchToggle(bool tog)
    {
        crouchToggle = tog;
    }
    public bool GetCrouch()
    {
        return crouch;
    }
    public void TakeDamage(float damage)
    {
        if (IframesDown)
        {
            HpBar.SetHP(currentHP - damage);
            SM.Play("PlayerHit");
            currentHP -= damage;
            IframesDown = false;
            StartCoroutine(IFrameCooldown());
            StartCoroutine(Flash());
        }
    }

    public void SetCurrentPickup(pickup up)
    {
        currentPick = up;
    }

    private IEnumerator Flash()
    {
        Color spriteColor = spriteRenderer.color;
        for(int i = 0; i < 3; i++)
        {
            spriteColor.a = 0.3f;
            spriteRenderer.color = spriteColor;
            yield return new WaitForSeconds(.1f);
            spriteColor.a = 1f;
            spriteRenderer.color = spriteColor;
            yield return new WaitForSeconds(.1f);
        }
    }

    private IEnumerator IFrameCooldown()
    {
        yield return new WaitForSeconds(.6f);
        IframesDown = true;
    }
    private IEnumerator DashAnimationCooldown()
    {
        yield return new WaitForSeconds(.1f);
        animator.SetBool("isDashing", false);
        yield return new WaitForSeconds(.1f);
        LockMovement(false);
    }
    private IEnumerator WaitDash()
    {
        yield return new WaitForSeconds((dashCooldown+ ModDashCooldown));
        dashIndObj.SetActive(false);
        canDash = true;
    }

    [ContextMenu("test Random Damge")]
    public void RandomTestDamage()
    {
        float dam = Random.Range(1, 5);
        Debug.Log("took " + dam  + " damage");
        TakeDamage(dam);
    }

    [ContextMenu("test 1 Damge")]
    public void TestDamage()
    {
        TakeDamage(1);
    }
}