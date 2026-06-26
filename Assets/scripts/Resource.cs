using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    private int maxCount;
    private int availableCount;
    public Color color;
    public List<(Process, ProcessColor)> connectedProcess;
    public bool isEmpty;
    public bool isColorSwitchCorrect;

    public List<(Process, ProcessColor)> waitingList;
    private List<IResourceComponent> components = new List<IResourceComponent>();
    private List<IResourceComponent> saveComponents = new List<IResourceComponent>();

    public void Initialize(int maxCount, Color color, List<IResourceComponent> components)
    {
        this.maxCount = maxCount;
        this.availableCount = maxCount;
        this.color = color;
        this.components = components;
        connectedProcess = new List<(Process, ProcessColor)>();
        waitingList = new List<(Process, ProcessColor)>();
    }

    public void OnMouseEnter()
    {
        GameManager.instance.ShowConnectedNumber(this);
        if(GameManager.instance.tutorialGameObject != null && GameManager.instance.tutorialResource == this)
        {
            GameManager.instance.tutorialGameObject.SetActive(true);
        }
    }

    public void OnMouseExit()
    {
        GameManager.instance.HideConnectedNumber(this);
        if (GameManager.instance.tutorialGameObject != null && GameManager.instance.tutorialResource == this)
        {
            GameManager.instance.tutorialGameObject.gameObject.SetActive(false);
        }
    }

    public State Connect(Process P, ProcessColor pc)
    {
        if (P.GetState() != State.RUNNING)
        {
            return State.FAULT;
        }

        if (availableCount > 0 && CheckConnectable(P, pc, true))
        {
            P.FinishedColorCountUp();
            availableCount--;

            for(int i = 0; i < components.Count; i++)
            {
                components[i].ConnectComponent(P, pc);
            }

            return State.RUNNING;
        }
        else
        {
            waitingList.Add((P, pc));
            P.Wait();
            return State.WAITING;
        }
    }

    public bool CheckConnectable(Process p, ProcessColor pc, bool isOnGame)
    {
        bool isCorrect;
        if (isOnGame == false && isEmpty) return true;

        isCorrect = color.Equals(pc.color);

        for (int i = 0; i < components.Count; i++)
        {
            isCorrect = components[i].CheckConnect(p, pc, isOnGame);
        }
        isColorSwitchCorrect = false;
        return isCorrect;
    }

    public Vector3 GetPositionForWaiting(Process P)
    {
        Vector2 v2 = transform.position - P.transform.position;
        Quaternion v3Rotattion = Quaternion.Euler(0, 0, Mathf.Atan2(v2.y, v2.x) * Mathf.Rad2Deg);
        Vector3 v3Distance = new Vector3(-1f, 0, 0);
        Vector3 v3Temp = v3Rotattion * v3Distance;
        return v3Temp + transform.localPosition;
    }

    public void TypeMatchAndActive()
    {
        transform.GetChild(maxCount - 1).gameObject.SetActive(true);
        for(int i = 0; i < components.Count; i++)
        {
            components[i].ActiveGameObject(maxCount, transform);
        }
    }

    public bool IsFinishable()
    {
        for(int i = 0; i < components.Count; i++)
        {
            if (components[i].IsFinishable() == false) return false;
        }
        return true;
    }

    public void FinishComponents(int round, Process P)
    {
        availableCount++;
        for (int i = 0; i < components.Count; i++) components[i].FinishComponent(round, P, this);
    }

    public (Process, ProcessColor) GetWaitingPandPC()
    {
        if (waitingList.Count != 0)
        {
            (Process, ProcessColor) ppc = waitingList[0];
            waitingList.RemoveAt(0);
            ppc.Item1.Run();
            return ppc;
        }
        return (null, null);
    }

    public List<IResourceComponent> GetComponents()
    {
        return components;
    }

    public int GetAvailableCount()
    {
        return availableCount;
    }

    public void SortConnectedProcessbyDistance()
    {
        connectedProcess.Sort((a, b) =>
        {
            int indexA = a.Item1.GetProcessColorList().IndexOf(a.Item2) - (a.Item1.GetProcessColorList().Count - a.Item1.selectedCount);
            int indexB = b.Item1.GetProcessColorList().IndexOf(b.Item2) - (b.Item1.GetProcessColorList().Count - b.Item1.selectedCount);
            if (indexA == indexB)
            {
                int index = Vector3.SqrMagnitude(a.Item1.transform.position - transform.position).CompareTo(Vector3.SqrMagnitude(b.Item1.transform.position - transform.position));
                if (index == 0) return a.Item2.order.CompareTo(b.Item2.order);
                else return index;
            }
            else return indexA.CompareTo(indexB);
        });
    }

    public void DoAfterRound(int round)
    {
        for(int i = 0; i < components.Count; i++)
        {
            components[i].DoAfterRound(round, this);
        }
    }

    public void DeepCopy()
    {
        for(int i = 0; i < components.Count; i++)
        {
            saveComponents.Add(components[i].DeepCopy());
        }
    }

    public void ReloadComponents()
    {
        components = saveComponents.ToList();
        saveComponents.Clear();
        availableCount = maxCount;
        waitingList.Clear();
        TypeMatchAndActive();
    }
}