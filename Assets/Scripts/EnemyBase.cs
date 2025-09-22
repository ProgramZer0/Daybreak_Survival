using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, IEnemy
{
    protected NavMeshAgent agent;
    protected Transform player;
    protected bool isStunned = false;
    protected PlayerInterface playerInterface;
    protected SoundManager SM;
    protected bool isDead = false;
    public float health = 5f;
    public float damage = 1;
    public float damageTime = 3f;
    public bool isDay = true;
    public AudioSource AD;
    public Sound deathSound;

    private bool hasBeenDamaged = false;

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

    protected void OnDeath()
    {
        isDead = true;
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            hasBeenDamaged = true;
            StartCoroutine(StartDamageCounter(damageTime));
            player.TakeDamage(damage);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!hasBeenDamaged)
        {
            collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
            if (player != null)
            {
                hasBeenDamaged = true;
                StartCoroutine(StartDamageCounter(damageTime));
                player.TakeDamage(damage);
            }
        }
    }

    public abstract void Tick();

    public abstract void TakeDamage(float damage, bool _isStunned = true);

    public GameObject GetGameObj()
    {
        return gameObject;
    }

    protected void PlaySound(Sound s, float timeWaiting = 0f)
    {
        StartCoroutine(PlaySoundWait(s, timeWaiting));
    }
    private IEnumerator PlaySoundWait(Sound s, float TIME)
    {
        yield return new WaitForSeconds(TIME);
        s.source = AD;
        s.source.clip = s.clip;
        s.source.loop = s.loop;
        s.source.volume = s.volume * SM.GetSoundMod();
        s.source.pitch = s.pitch;
        s.source.Play();
    }

    private IEnumerator StartDamageCounter(float TIME)
    {
        yield return new WaitForSeconds(TIME);
        hasBeenDamaged = false;
    }
}