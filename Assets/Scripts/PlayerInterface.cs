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

    [SerializeField] private Weapon currentWeapon;

    [SerializeField] int weaponAmmo = 0;
    [SerializeField] int maxAmmo = 0;


    private Rigidbody2D body;
    private Direction facing = Direction.N;
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
    }

    private void Update()
    {

        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Mouse0)) attacking = true;
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
            Animator.SetBool("Running", false);
            walkingSound.SetActive(false);
            return;
        }         

        Vector2 moveAmount = new Vector2(inputH, inputV).normalized;

        if (attacking && canShoot)
        {
            tryAttacking();
            attacking = false;
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

        Vector2 mouseScreenPos = Input.mousePosition;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector2(mouseScreenPos.x, mouseScreenPos.y));
        GameObject o = GameObject.Instantiate(projectile, gunPoss[0].transform.position, Quaternion.identity);
        Vector2 gun = new Vector2(gunPoss[0].transform.position.x, gunPoss[0].transform.position.y);
        Vector2 moveDirection = (mouseWorldPos - gun).normalized;

        //Debug.Log(moveDirection.ToString());
        o.GetComponent<Rigidbody2D>().linearVelocity = moveDirection * currentWeapon.projectileSpeed;
        o.GetComponent<Projectile>().damage = currentWeapon.projectileDamage;
        canShoot = false;
        weaponAmmo--;
        StartCoroutine(ShootingCoooldown());
    }

    private IEnumerator ShootingCoooldown()
    {
        yield return new WaitForSeconds(coolDown);
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

                break;
            case (Direction.SE):

                break;
            case (Direction.NE):

                break;
            case (Direction.W):

                break;
            case (Direction.SW):

                break;
            case (Direction.NW):

                break;
            case (Direction.S):

                break;
            case (Direction.N):

                break;
        }
            
        if (moving)
        {
            walkingSound.SetActive(true);
        }
        else
        {
            walkingSound.SetActive(false);
        }

        Animator.SetBool("Running", moving);
        Animator.SetBool("Dash", dashed);

        if (dashed)
        {
            //dash
            SM.Play("Dash");
            dashing = false;
        }
        else
        {
            Animator.SetBool("Dash", false);
        }
        dashing = false;
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