using System.Collections.Generic;
using UnityEngine;

public class LifeStyleController : MonoBehaviour
{
    [SerializeField] private PlayerInterface player;
    [SerializeField] private int maxLifestyles = 10;
    public GameObject toolTipPreFab;

    public LifeStyles[] allLifestyles;
    public List<LifeStyles> lifeStylesAvailable;
    private List<LifeStyles> lifeStylesActive;
    public void InitLists()
    {
        lifeStylesAvailable = new List<LifeStyles>();
        lifeStylesActive = new List<LifeStyles>();
    }
    public bool CheckIfFull()
    {
        if (lifeStylesActive.Count >= maxLifestyles) return true;
        else return false;
    }

    public bool MakeLifestylesActive(LifeStyles ls)
    {
        if (lifeStylesActive.Count >= maxLifestyles) return false;

        if (!lifeStylesActive.Contains(ls))
        {
            lifeStylesActive.Add(ls); 
            return true;
        }
        return false;
    }

    public void DeActiveLifestyles(LifeStyles ls)
    {
        if (lifeStylesActive.Contains(ls))
            lifeStylesActive.Remove(ls);
    }

    public void AddAllActive()
    {
        if (lifeStylesActive == null) return;
        if (lifeStylesActive.Count <= 0) return;

        foreach (LifeStyles ls in lifeStylesActive)
            AddLifestyle(ls);
    }

    private void AddLifestyle(LifeStyles LS)
    {
        if (LS.script == null)
        {
            Debug.LogError("No script assigned to lifestyle: " + LS.lifestyleName);
            return;
        }

        var type = LS.script.GetClass();
        if (type == null)
        {
            Debug.LogError("Could not resolve script for: " + LS.lifestyleName);
            return;
        }

        if (gameObject.GetComponent(type) != null)
            return;

        var comp = player.gameObject.AddComponent(type) as lifestyleScript;
        if (comp == null)
        {
            Debug.LogError("Script is not a lifestyleScript: " + LS.script.name);
            return;
        }

        comp.Initialize(player, LS);
    }

    public void ClearAll()
    {
        lifeStylesActive = new List<LifeStyles>();
        lifeStylesAvailable = new List<LifeStyles>();
    }
    public LifeStyles[] GetAllLifeStyles()
    {
        allLifestyles = Resources.LoadAll<LifeStyles>("Scriptables/LifeStyles");
        return allLifestyles;
    }
    public List<LifeStyles> GetActiveLifestyles()
    {
        return lifeStylesActive;
    }
}