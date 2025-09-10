using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float timer = 0.00001f;
    private float alpha = 0;
    private Color spriteColor;
    private float FallOffMultiplier = 1;
    private float FallOffTime = 0;
    private float projectileTime  = 8;
    private float splashRange = 0;  
    private bool splashDamge = false;
    private float damage = 0;
    private float fallDamage = 0;
    private float appearTime = 0f;
    private float fadeTime = 0.1f;
    private float FOM_MULTIPLY = 0.01f;
    private bool hasAnimation;
    [SerializeField] private SpriteRenderer renderer;

    private void Awake()
    {

        spriteColor = renderer.color;
        spriteColor.a = 0.4f;
        renderer.color = spriteColor;
    }


    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= appearTime && timer < fadeTime) alpha = 0.7f;

        if (timer > fadeTime) alpha = 1f;


        spriteColor.a = alpha;
        renderer.color = spriteColor;

        if (timer <= FallOffTime)
            fallDamage = damage;
        else
        {
            float decayRate = FallOffMultiplier / (projectileTime - FallOffTime);
            float elapsedSinceFalloff = timer - FallOffTime;

            fallDamage = damage * Mathf.Exp(-decayRate * elapsedSinceFalloff);
        }

        if (timer > projectileTime)
        {
            timer = -100000;
            if (hasAnimation)
                ExplodeAnim();
            else
                Explode();
        }
    }

    public void SetValues(float FOM, float time, float range, bool splash, float d, float FOMTime, bool projectileHasAnimation, float appearT, float fTime)
    {
        hasAnimation = projectileHasAnimation;
        FallOffTime = FOMTime;
        damage = d;
        FallOffMultiplier = FOM;
        projectileTime = time;
        splashDamge = splash;
        splashRange = range;
        appearTime = appearT;
        fadeTime = fTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.name);
        collision.gameObject.TryGetComponent<IEnemy>(out IEnemy enemy);
        if(enemy != null)
        {
            enemy.TakeDamage(fallDamage);
        }

        gameObject.GetComponent<Collider2D>().enabled = false;
        if (hasAnimation)
            ExplodeAnim();
        else
            Explode();

    }

    private IEnumerator ExplodeAnim()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (splashDamge)
            Splash();
        yield return new WaitForSeconds(0.08f);
        Destroy(transform.gameObject);
    }
    private void Explode()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (splashDamge)
            Splash();
        Destroy(transform.gameObject);
    }
    private void Splash()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRange, FindFirstObjectByType<PlayerInterface>().EnemyLayer);
        foreach (Collider2D hit in hits)
        {   
            if (hit.TryGetComponent<IEnemy>(out IEnemy enemy))
            {
                enemy.TakeDamage(fallDamage); 
            }
        }
    }
}
