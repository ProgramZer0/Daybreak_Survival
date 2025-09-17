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
    [SerializeField] private float speed = 4f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float dashCooldown = 10f;
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private float pickupRange = 1f;
    [SerializeField] private float playerPickupTime = 3f;
    [SerializeField] private float maxHP = 10f;
    [SerializeField] private float hordeForgetTime = 5f;

    [SerializeField] private LayerMask interactLayer;
    public LayerMask EnemyLayer;
    [SerializeField] private LayerMask meleeHitLayer;
    [SerializeField] private Animator Animator;
    [SerializeField] private GameObject walkingSound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject[] gunPoss;
    [SerializeField] private GameObject[] shootingLights;
    

    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject downObj;
    [SerializeField] private GameObject upObj;
    [SerializeField] private GameObject leftObj;
    [SerializeField] private GameObject rightObj;
    [SerializeField] private GameObject mainPlayerObj;
    [SerializeField] private GameObject dashIndObj;
    [SerializeField] private GameObject dashObj;

    [SerializeField] private GameObject pickupObj;

    [SerializeField] private Weapon currentWeapon;

    [SerializeField] private int weaponAmmo = 0;
    [SerializeField] private int maxAmmo = 0;


    private Rigidbody2D body;
    private Direction facing = Direction.S;
    private bool attacking = false;
    private bool meleeing = false;
    private bool interact = false;
    private bool dashing = false;
    private bool crouch = false;
    private bool pickupKey = false;
    private bool canShoot = true;
    private bool canMelee = true;
    private bool canDash = true;
    private bool IframesDown = true;
    private bool cannotMove = true;
    private bool crouchToggle = true;
    private bool isShooting = false;
    private bool isSeenAndChased = false;
    private float inputH = 0f;
    private float inputV = 0f;
    private float currentHP = 10;
    private float pickupTimer = 0;
    private float chaseTimer = 0;
    private pickup currentPick;

    public float GetMaxHP() { return maxHP; }
    public float GetCurrentHP() { return currentHP; }
    public float GetCooldown() { return dashCooldown; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        resetPlayerData();
    }

    public void resetPlayerData()
    {
        currentHP = maxHP;
        weaponAmmo = 0;
        maxAmmo = 0;
        currentWeapon = GUI.ReturnEmptyWeapon();
        DisableAllWalks();
    }

    private void Update()
    {
        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Mouse0)) attacking = true;
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

        if (Input.GetKeyDown(KeyCode.LeftShift)) dashing = true;

        if (isSeenAndChased)
        {
            chaseTimer += Time.deltaTime;
        }
        if(chaseTimer >= hordeForgetTime)
        {
            isSeenAndChased = false;
            chaseTimer = 0;
        }
    }
    
    public Weapon GetActiveWeapon()
    {
        return currentWeapon;
    }

    private void FixedUpdate()
    {
        

        if(currentHP < 0)
        {
            GM.EndGameFail();
        }

        if (cannotMove) 
        {
            walkingSound.SetActive(false);
            return;
        }

        if (currentPick != null)
            TryPickUp();
        else
            FindFirstObjectByType<MainGUIController>().StopSeeingPickup();

        Vector2 moveAmount = new Vector2(inputH, inputV).normalized;
        if (meleeing && canMelee)
        {
            TryMelee();
            meleeing = false;
        }

        if (attacking && canShoot)
        {
            TryAttacking();
        }

        if (interact)
        {
            SM.Play("Interact");
            TryInteract();
        }

        Move(moveAmount, dashing);
    }

    private void TryPickUp()
    {
        if (Vector2.Distance(currentPick.gameObject.transform.position, transform.position) <= pickupRange)
        {
            if (pickupKey)
            {
                pickupTimer += Time.deltaTime;
                if (!pickupObj.activeSelf)
                {
                    pickupObj.SetActive(true);
                    FindFirstObjectByType<MainGUIController>().SeePickup(currentPick.weapon.pickupTime);
                    pickupObj.GetComponent<PickupIconScript>().triggerPickup();
                }

                if (pickupTimer >= currentPick.weapon.pickupTime)
                {
                    currentPick.addPickup(gameObject);
                    FindFirstObjectByType<MainGUIController>().StopSeeingPickup();
                    pickupTimer = 0;
                }
            }
            else
            {
                FindFirstObjectByType<MainGUIController>().StopSeeingPickup();
                pickupTimer = 0;
            }
        }
        else
        {
            currentPick = null;
            pickupTimer = 0;
            FindFirstObjectByType<MainGUIController>().StopSeeingPickup();
        }
    }

    private void TryAttacking()
    {
        isShooting = false;
        if (GUI.GetInUI())
            return;
        if (weaponAmmo <= 0)
        {
            SM.Play("Click");
            currentWeapon = GUI.ReturnEmptyWeapon();
            return;
        }

        SM.Play("Fire");
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

        SM.Play("Fire");
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

        IEnemy en = FanCheck(dir);
        if(en != null)
        {
            float damage = 0f;

            if (currentWeapon == null)
                damage = 1f;
            else
                damage = currentWeapon.meleeDamage;

            en.TakeDamage(damage);
            canMelee = false;

            StartCoroutine(ShootingCoooldown(currentWeapon.meleeCooldown));
        }   
    }
    private IEnemy FanCheck(Vector2 facingDir)
    {
        float startAngle = -currentWeapon.meleeFOV / 2f;
        float step = currentWeapon.meleeFOV / (currentWeapon.meleeRays - 1);

        for (int i = 0; i < currentWeapon.meleeRays; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * facingDir;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, currentWeapon.meleeRange, meleeHitLayer);
            if (hit && hit.transform.TryGetComponent<IEnemy>(out IEnemy enemy))
                return enemy;
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
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, interactLayer);
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

    private void Move(Vector2 move, bool dashed)
    {
        bool moving = true;
        float speeds = speed;
        if (crouch)
            speeds = crouchSpeed;

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

        switch (facing)
        {
            case (Direction.E):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(false);
                    rightObj.SetActive(true);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = rightSprite;
                facingInt = 6;
                break;
            case (Direction.SE):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = downSprite;
                facingInt = 5;
                break;
            case (Direction.NE):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = upSprite;
                facingInt = 7;
                break;
            case (Direction.W):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(true);
                }
                else
                    spriteRenderer.sprite = leftSprite;
                facingInt = 2;
                break;
            case (Direction.SW):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = downSprite;
                facingInt = 3;
                break;
            case (Direction.NW):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = upSprite;
                facingInt = 1;
                break;
            case (Direction.S):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = downSprite;
                facingInt = 4;
                break;
            case (Direction.N):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                }
                else
                    spriteRenderer.sprite = upSprite;
                facingInt = 0;
                break;
        }

        DisableCrossEnablex(facingInt);

        if (moving)
        {
            mainPlayerObj.SetActive(false);
            walkingSound.SetActive(true);
        }
        else
        {
            DisableAllWalks();
            walkingSound.SetActive(false);
        }

        if (dashed && canDash)
        {
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

            LockMovement(true);
            SM.Play("Dash");

            GameObject o = GameObject.Instantiate(dashObj, transform.position, Quaternion.Euler(0, 0, (facingside * 45)));

            StartCoroutine(DashMovement(gunPoss[facingInt].transform.position));
            canDash = false;
            dashIndObj.SetActive(true);
            dashIndObj.GetComponent<DashIndicator>().SetCooldown(dashCooldown);
            dashIndObj.GetComponent<DashIndicator>().TriggerCooldown();
            StartCoroutine(WaitDash());
        }
        dashing = false;
    }

    public string GetAmmo()
    {
        return  weaponAmmo.ToString() + "/" + maxAmmo.ToString();
    }

    public bool GetIsSeenAndChased() { return isSeenAndChased; }
    public void SetIsSeenAndChased(bool s) { isSeenAndChased = s; }

    public bool GetShottingBool() { return isShooting; }
    private void DisableAllWalks()
    {
        downObj.SetActive(false);
        upObj.SetActive(false);
        rightObj.SetActive(false);
        leftObj.SetActive(false);
        mainPlayerObj.SetActive(true);
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
        if(currentWeapon != null && currentWeapon != FindFirstObjectByType<MainGUIController>().ReturnEmptyWeapon())
        {
            //Debug.Log("spawning weapon");
            GameObject o = GameObject.Instantiate(currentWeapon.prefab, transform.position, Quaternion.identity);
            o.GetComponent<pickup>().currentAmmo = weaponAmmo;
        }

        currentWeapon = weapon;
        weaponAmmo = ammo;
        maxAmmo = weapon.maxAmmo;
        FindFirstObjectByType<WeaponHUD>().SetCurrentWeapon(currentWeapon);
        FindFirstObjectByType<CameraController>().maxScrollOut = weapon.weaponZoom;
        FindFirstObjectByType<CameraController>().minScrollOut = weapon.weaponMinZoom;

        return true;
    }
    public bool AddAmmo(WeaponAmmoType type, int amount)
    {
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
            currentHP -= damage;
            FindFirstObjectByType<HPBar>().SetHP(currentHP);
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
    private IEnumerator DashMovement(Vector2 dashLoc)
    {
        yield return new WaitForSeconds(.05f);
        transform.position = dashLoc;
        if(!GUI.GetInUI())
            LockMovement(false);
    }
    private IEnumerator WaitDash()
    {
        yield return new WaitForSeconds(dashCooldown);
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