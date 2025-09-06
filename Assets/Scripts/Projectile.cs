using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float timer = 0;
    private float alpha = 0;
    private Color spriteColor;

    [SerializeField] private SpriteRenderer renderer;

    public float damage = 0;
    private void Awake()
    {

        spriteColor = renderer.color;
        spriteColor.a = 0.7f;
        renderer.color = spriteColor;
    }


    void Update()
    {
        timer += Time.deltaTime;

        alpha = timer / 1.3f;

        if (alpha > 1) alpha = 1;

        if (alpha < .7) alpha = 0.3f;

        spriteColor.a = alpha;

        renderer.color = spriteColor;

        if (timer > 8)
        {
            timer = -100000;
            Explode();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.name);
        collision.gameObject.TryGetComponent<IEnemy>(out IEnemy enemy);
        if(enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        gameObject.GetComponent<Collider2D>().enabled = false;
        Explode();
    }

    private void Explode()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        Destroy(transform.gameObject);
    }
}
