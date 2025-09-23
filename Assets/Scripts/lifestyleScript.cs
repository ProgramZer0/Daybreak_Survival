using UnityEngine;

public abstract class lifestyleScript : MonoBehaviour
{
    protected bool useUpdate = false;
    protected bool hasMax = false;
    protected bool isActive = false;
    protected int maxTriggers = 1;
    protected float timeToTrigger = 0;

    private float timer = 0;
    private int timesTriggered = 0;
    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        if (timer >= timeToTrigger)
            if (useUpdate && TriggerLifestyle())
                timer = 0f; 
    }

    private bool CheckLim()
    {
        if (hasMax)
            if (timesTriggered >= maxTriggers)
                return false;

        return true;
    }

    public bool TriggerLifestyle()
    {
        if (CheckLim())
        {
            Tick();
            timesTriggered++;
            return true;
        }
        else
            return false;
    }
    protected abstract void Tick();
    public abstract void Initialize(PlayerInterface player, LifeStyles lifestlye);
    public void SetUseUpdate(bool update) { useUpdate = update; }
    public void SetUseLimit(bool isUsed) { hasMax = isUsed; }
    public int GetLimit() { return maxTriggers; }
    public void SetLimit(int lim) { maxTriggers = lim; }


}
