using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, IEnemy
{
    protected NavMeshAgent agent;
    protected Transform player;
    protected bool isStunned = false;
    protected PlayerInterface playerInterface;
    protected SoundManager SM;
    public float health = 5f;
    public float damage = 1;
    public bool isDay = true;
    public AudioSource AD;
    public Sound deathSound;

    public virtual void Initialize(GameObject player, bool _isDay, SoundManager _SM)
    {
        this.player = player.transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        playerInterface = player.GetComponent<PlayerInterface>();
        SM = _SM;
        isDay = _isDay;
    }
    public void Initialize(GameObject player)
    {
        this.player = player.transform;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        playerInterface = player.GetComponent<PlayerInterface>();
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
        if (agent != null && player != null)
            agent.SetDestination(player.position);
    }

    public abstract void TakeDamage(float damage, bool _isStunned);

    public GameObject GetGameObj()
    {
        return gameObject;
    }

    public void PlaySound(Sound s)
    {
        s.source = AD;
        s.source.clip = s.clip;
        s.source.loop = s.loop;

        s.source.volume = s.volume * SM.GetSoundMod();
        s.source.pitch = s.pitch;
        s.source.Play();
    }
}