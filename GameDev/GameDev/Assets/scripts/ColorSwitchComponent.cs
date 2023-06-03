using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSwitchComponent : IResourceComponent
{
    public List<Color> colors;
    public int thisColorIndex = 0;
    public Process sameProcess;

    public Transform thisTransform;

    private List<Process> connectedProcesses;

    public ColorSwitchComponent(List<Color> colors, Transform thisTransform)
    {
        connectedProcesses = new List<Process>();
        this.colors = colors;
        this.thisTransform = thisTransform;
    }

    public bool CheckConnect(Process P, ProcessColor pc, bool isOnGame)
    {
        if (isOnGame == false && pc.r.isEmpty) return true;
        if (isOnGame == false)
        {
            pc.r.isColorSwitchCorrect = colors.Contains(pc.color);
            return pc.r.isColorSwitchCorrect;
        }
        else return pc.r.color.Equals(pc.color);
    }

    public void ActiveGameObject(int maxCount, Transform parent)
    {
        parent.GetComponent<Resource>().color = colors[0];
        parent.GetComponent<SpriteRenderer>().color = colors[0];
        thisTransform.gameObject.SetActive(true);
        for(int i = 0; i < thisTransform.childCount; i++)
        {
            if (i < colors.Count) thisTransform.GetChild(i).GetComponent<SpriteRenderer>().color = colors[i];
            else thisTransform.GetChild(i).GetComponent<SpriteRenderer>().color = colors[0];
        }

    }

    public bool IsFinishable()
    {
        return true;
    }

    public void ConnectComponent(Process P, ProcessColor pc)
    {
        if (connectedProcesses.Contains(P) == false) connectedProcesses.Add(P);
    }

    public void FinishComponent(int round, Process P, Resource parent)
    {
        if (sameProcess == null || sameProcess.Equals(P) == false)
        {
            if (connectedProcesses.Count <= 1)
            {
                thisColorIndex++;
                if (thisColorIndex >= colors.Count) thisColorIndex = 0;

                parent.GetComponent<SpriteRenderer>().color = parent.color = colors[thisColorIndex];

                for (int i = colors.Count; i < thisTransform.childCount; i++)
                {
                    thisTransform.GetChild(i).GetComponent<SpriteRenderer>().color = colors[thisColorIndex];
                }
            }

            connectedProcesses.Remove(P);
        }
        sameProcess = P;

        int sameColorCount = 0;
        for(int i = 0; i < parent.waitingList.Count; i++)
        {
            if (parent.waitingList[i].Item2.color.Equals(parent.color))
            {
                if(i != 0)
                {
                    (Process, ProcessColor) temp = parent.waitingList[i];
                    parent.waitingList.RemoveAt(i);
                    parent.waitingList.Insert(0, temp);
                }

                sameColorCount++;
            }
            if (sameColorCount == parent.GetAvailableCount()) break;
        }

        if (parent.GetAvailableCount() > 1)
        {
            while (sameColorCount > 1)
            {
                (Process, ProcessColor) temp = parent.GetWaitingPandPC();
                if (temp.Item1 != null)
                {
                    GameManager.instance.MoveToNextRound(new Todo(temp.Item1, temp.Item2), round, true);
                }
                sameColorCount--;
            }

        }
    }

    public void DoAfterRound(int round, Resource resource) { }

    public IResourceComponent DeepCopy()
    {
        ColorSwitchComponent temp = new ColorSwitchComponent(colors, thisTransform);
        return temp;
    }
}
