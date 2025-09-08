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
    [SerializeField] private float speed = 10;
    [SerializeField] private float attackSpeed = 1;
    [SerializeField] private float damage = 1;
    [SerializeField] private float coolDown = 0.3f;

    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Animator Animator;
    [SerializeField] private GameObject walkingSound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject[] gunPoss;
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private SoundManager SM;
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

    [SerializeField] int weaponAmmo = 0;
    [SerializeField] int maxAmmo = 0;


    private Rigidbody2D body;
    private Direction facing = Direction.S;
    private bool attacking = false;
    private bool interact = false;
    private bool dashing = false;
    private bool crouch = false; 
    private bool canShoot = true;
    private bool cannotMove = false;
    private float inputH = 0f;
    private float inputV = 0f;
    private float currentHP = 10;


    private void Awake()
    {
        currentWeapon = GUI.ReturnEmptyWeapon();
        body = GetComponent<Rigidbody2D>();
        downObj.SetActive(false);
        upObj.SetActive(false);
        leftObj.SetActive(false);
        rightObj.SetActive(false);
        mainPlayerObj.SetActive(true);
    }

    private void Update()
    {

        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Mouse0)) attacking = true;
        if (Input.GetKeyUp(KeyCode.Mouse0)) attacking = false;
        if (Input.GetKeyDown(KeyCode.F)) interact = true;
        if (Input.GetKeyDown(KeyCode.C)) crouch = true;
        if (Input.GetKeyDown(KeyCode.LeftShift)) dashing = true;
    }
    
    private void FixedUpdate()
    {
        if(currentHP < 0)
        {
            //end game
        }

        if (cannotMove) 
        {
            walkingSound.SetActive(false);
            return;
        }         

        Vector2 moveAmount = new Vector2(inputH, inputV).normalized;

        if (attacking && canShoot)
        {
            tryAttacking();
        }

        if (interact)
        {
            SM.Play("Interact");
            TryInteract();
        }

        Move(moveAmount, false, dashing);
    }

    private void tryAttacking()
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

        Vector3 spawnPos = gunPoss[facingside].transform.position;
        GameObject o = GameObject.Instantiate(projectile, transform.position, Quaternion.Euler(0, 0, (facingside*45)));
        Vector2 moveDirection = (spawnPos - transform.position).normalized;

        o.GetComponent<Rigidbody2D>().linearVelocity = moveDirection * currentWeapon.projectileSpeed;
        o.GetComponent<Projectile>().damage = currentWeapon.projectileDamage;
        canShoot = false;

        weaponAmmo--;
        StartCoroutine(ShootingCoooldown(currentWeapon.projectileCooldown));
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

    private void Move(Vector2 move, bool crouch, bool dashed)
    {
        bool moving = true;
        body.linearVelocity = move * speed;

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
                    DisableCrossEnablex(6);
                }
                else
                    spriteRenderer.sprite = rightSprite;
                break;
            case (Direction.SE):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(5);
                }
                else
                    spriteRenderer.sprite = downSprite;
                break;
            case (Direction.NE):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(7);
                }
                else
                    spriteRenderer.sprite = upSprite;
                break;
            case (Direction.W):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(true);
                    DisableCrossEnablex(2);
                }
                else
                    spriteRenderer.sprite = leftSprite;
                break;
            case (Direction.SW):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(3);
                }
                else
                    spriteRenderer.sprite = downSprite;
                break;
            case (Direction.NW):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(1);
                }
                else
                    spriteRenderer.sprite = upSprite;
                break;
            case (Direction.S):
                if (moving)
                {
                    downObj.SetActive(true);
                    upObj.SetActive(false);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(4);
                }
                else
                    spriteRenderer.sprite = downSprite;
                break;
            case (Direction.N):
                if (moving)
                {
                    downObj.SetActive(false);
                    upObj.SetActive(true);
                    rightObj.SetActive(false);
                    leftObj.SetActive(false);
                    DisableCrossEnablex(0);
                }
                else
                    spriteRenderer.sprite = upSprite;
                break;
        }


        if (moving)
        {
            mainPlayerObj.SetActive(false);
            walkingSound.SetActive(true);
        }
        else
        {
            disableAllWalks();
            walkingSound.SetActive(false);
        }


        if (dashed)
        {
            //dash
            SM.Play("Dash");
            Animator.Play("Dash");
            dashing = false;
        }
        dashing = false;
    }

    private void disableAllWalks()
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

    public bool AddWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        weaponAmmo = weapon.maxAmmo;
        maxAmmo = weapon.maxAmmo;
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


    public void TakeDamage(float damage)
    {
        currentHP -= damage;
    }
}