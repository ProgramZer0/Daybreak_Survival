using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, IEnemy 
{
    protected NavMeshAgent agent;
    protected Transform player;

    public float health = 5f;
    public float damage = 1;
    public bool isDay = true;

    public virtual void Initialize(GameObject player)
    {
        this.player = player.transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }
    public virtual void OnDeath()
    {
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    public abstract void Tick(); 

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0) OnDeath();
    }
}