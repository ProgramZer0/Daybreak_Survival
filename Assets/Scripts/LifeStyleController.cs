using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeStyleController : MonoBehaviour
{
    [SerializeField] private PlayerInterface player;
    [SerializeField] private LifeStyles[] allLSs;
    [SerializeField] private int maxLifestyles = 10;

    public List<LifeStyles> lifeStylesAvalible;
    private List<LifeStyles> LifeStylesActive;
    private int LifestylesActive = 0;

    private void Start()
    {
        LifeStylesActive = new List<LifeStyles>();
        lifeStylesAvalible = new List<LifeStyles>();
    }

    public void MakeLifestylesActive(LifeStyles ls)
    {
        if (LifestylesActive > maxLifestyles) return;
        bool found = false;
        foreach (LifeStyles l in LifeStylesActive)
        {
            if (l == ls)
                found = true;
        }

        if (!found)
        {
            LifeStylesActive.Add(ls);
            LifestylesActive++;
        }
    }
    public void DeActiveLifestyles(LifeStyles ls)
    {
        bool found = false;
        foreach(LifeStyles l in LifeStylesActive)
        {
            if (l == ls)
                found = true;
        }

        if(found)
            LifeStylesActive.Remove(ls);
    }

    public void AddAllActive()
    {
        foreach (LifeStyles ls in LifeStylesActive)
            AddLifestyle(ls);
    }
    private void AddLifestyle(LifeStyles LS)
    {
        string scriptName = LS.scriptName;
        var type = System.Type.GetType(scriptName);

        if (type == null)
        {
            Debug.LogError("Type not found: " + scriptName);
            return;
        }

        var comp = gameObject.AddComponent(type) as lifestyleScript;

        if (comp == null)
        {
            Debug.LogError("Type is not a lifestyleScript: " + scriptName);
            return;
        }

        comp.Initialize(player, LS);
    }
}
