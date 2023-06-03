using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EmptyColorComponent : IResourceComponent
{
    public bool isColorFixed;
    public Resource resource;
    public Color baseColor;
    bool isFirst;

    public EmptyColorComponent(Resource r)
    {
        isFirst = true;
        resource = r;
        isColorFixed = false;
    }

    public bool CheckConnect(Process P, ProcessColor pc, bool isOnGame)
    {
        if (isColorFixed == false) return true;
        else return pc.r.color.Equals(pc.color);
    }

    public void ActiveGameObject(int maxCount, Transform parent)
    {
        if (isFirst)
        {
            baseColor = resource.color;
        }
        else
        {
            resource.color = baseColor;
            resource.transform.GetComponent<SpriteRenderer>().color = baseColor;
        }
        resource.isEmpty = true;
    }

    public bool IsFinishable()
    {
        return true;
    }

    public void ConnectComponent(Process P, ProcessColor pc)
    {
        if (isColorFixed == false)
        {
            pc.r.color = pc.color;
            pc.r.GetComponent<SpriteRenderer>().DOColor(pc.color, .4f);

            for(int i = 0; i < pc.r.GetComponents().Count; i++)
            {
                if(pc.r.GetComponents()[i] is ColorSwitchComponent)
                {
                    (pc.r.GetComponents()[i] as ColorSwitchComponent).colors[0] = pc.color;
                    (pc.r.GetComponents()[i] as ColorSwitchComponent).ActiveGameObject(-1, null);
                    break;
                }
            }
            resource.isEmpty = false;
            isColorFixed = true;
        }
    }

    public void FinishComponent(int round, Process P, Resource parent)
    {

    }

    public void DoAfterRound(int round, Resource resource)
    {
         
    }

    public IResourceComponent DeepCopy()
    {
        EmptyColorComponent temp = new EmptyColorComponent(resource);
        temp.isFirst = false;
        temp.baseColor = baseColor;
        return temp;
    }
}
