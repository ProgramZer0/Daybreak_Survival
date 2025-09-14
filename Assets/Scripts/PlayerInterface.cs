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
    [SerializeField] private float maxHP = 10f;
    [SerializeField] private LayerMask interactLayer;
    public LayerMask EnemyLayer;
    [SerializeField] private Animator Animator;
    [SerializeField] private GameObject walkingSound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject[] gunPoss;

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

    [SerializeField] private Weapon currentWeapon;

    [SerializeField] private int weaponAmmo = 0;
    [SerializeField] private int maxAmmo = 0;


    private Rigidbody2D body;
    private Direction facing = Direction.S;
    private bool attacking = false;
    private bool interact = false;
    private bool dashing = false;
    private bool crouch = false; 
    private bool canShoot = true;
    private bool canDash = true;
    private bool IframesDown = true;
    private bool cannotMove = true;
    private bool crouchToggle = true;
    private float inputH = 0f;
    private float inputV = 0f;
    private float currentHP = 10;

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
        if (Input.GetKeyDown(KeyCode.F)) interact = true;
        
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

        Vector2 moveAmount = new Vector2(inputH, inputV).normalized;

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

    private void TryAttacking()
    {

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

        for(int i = 0; i < currentWeapon.projectileAmount; i++)
        {
            Vector3 gunDirection = gunPoss[facingside].transform.position;
            Vector2 randomOffset = Random.insideUnitCircle * currentWeapon.spawnSpread;
            Vector2 spawnPos = transform.position + (Vector3)randomOffset;
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
        
        canShoot = false;

        weaponAmmo--;
        StartCoroutine(ShootingCoooldown(currentWeapon.projectileCooldown));
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

    private IEnumerator ShootingCoooldown(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canShoot = true;
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
            LockMovement(true);
            SM.Play("Dash");
            //dash animation
            StartCoroutine(DashMovement(gunPoss[facingInt].transform.position));
            canDash = false;
            FindFirstObjectByType<DashIndicator>().TriggerCooldown();
            StartCoroutine(WaitDash());
        }
        dashing = false;
    }

    public string GetAmmo()
    {
        return  weaponAmmo.ToString() + "/" + maxAmmo.ToString();
    }

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
    public bool AddWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        weaponAmmo = weapon.maxAmmo;
        maxAmmo = weapon.maxAmmo;
        FindFirstObjectByType<WeaponHUD>().SetCurrentWeapon(currentWeapon);
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