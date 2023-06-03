using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimultaneousComponent : IResourceComponent
{
    private List<Process> readyList;
    private int maxCount;
    public bool isFull;

    public Transform thisTransform;

    public SimultaneousComponent(int maxCount, Transform thisTransform)
    {
        this.maxCount = maxCount;
        this.thisTransform = thisTransform;
        readyList = new List<Process>();
    }

    public bool CheckConnect(Process P, ProcessColor pc, bool isOnGame)
    {
        return pc.r.color.Equals(pc.color);
    }

    public void ActiveGameObject(int maxCount, Transform parent)
    {
        thisTransform.gameObject.SetActive(true);
    }

    public void ConnectComponent(Process P, ProcessColor pc)
    {
        P.SimultanCountUp();
        readyList.Add(P);
    }

    public bool IsFinishable()
    {
        if (isFull == true) return true;
        else if(readyList.Count == maxCount)
        {
            isFull = true;
            return true;
        }
        return false;
    }

    public void FinishComponent(int round, Process P, Resource parent)
    {
        P.SimultanCountDown();
        readyList.Remove(P);
    }

    public void DoAfterRound(int round, Resource resource)
    {
        isFull = false;
    }

    public IResourceComponent DeepCopy()
    {
        SimultaneousComponent temp = new SimultaneousComponent(maxCount, thisTransform);
        return temp;
    }
}
