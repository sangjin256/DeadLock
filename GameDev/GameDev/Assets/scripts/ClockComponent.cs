using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class ClockComponent : IResourceComponent
{
    public Transform thisTransform;
    public SpriteRenderer parentSprite;
    public TextMeshPro tmp;
    public bool isEnableFirst;
    public bool isEnable;
    public int clockNum;
    public List<Process> clockWaitingList;

    public bool isTimeDone;

    public ClockComponent(bool isEnable, int clockNum, Transform thisTransform)
    {
        clockWaitingList = new List<Process>();
        isTimeDone = false;
        this.clockNum = clockNum;
        this.isEnableFirst = isEnable;
        this.isEnable = isEnable;
        this.thisTransform = thisTransform;
    }

    public void ActiveGameObject(int maxCount, Transform parent)
    {
        parentSprite = parent.GetComponent<SpriteRenderer>();
        if (isEnable == false) parentSprite.color = new Color(parentSprite.color.r, parentSprite.color.g, parentSprite.color.b, .4f);
        else parentSprite.color = new Color(parentSprite.color.r, parentSprite.color.g, parentSprite.color.b, 1.0f);
        tmp = thisTransform.GetChild(0).GetComponent<TextMeshPro>();
        tmp.text = clockNum.ToString();
        thisTransform.gameObject.SetActive(true);
    }

    public bool IsFinishable()
    {
        return true;
    }

    public bool CheckConnect(Process P, ProcessColor pc, bool isOnGame)
    {
        if (isEnable == false && isOnGame == false) return pc.r.isEmpty || pc.r.isColorSwitchCorrect || pc.r.color.Equals(pc.color);
        if (isEnable || isOnGame == false) return pc.r.isEmpty || pc.r.color.Equals(pc.color);
        if (isTimeDone == false)
        {
            P.isClockWaiting = true;
            clockWaitingList.Add(P);
        }
        return false;
    }

    public void ConnectComponent(Process P, ProcessColor pc)
    {
    }

    public void FinishComponent(int round, Process P, Resource parent)
    {

    }

    public void DoAfterRound(int round, Resource resource)
    {
        if (isTimeDone == false)
        {
            --clockNum;
            tmp.text = clockNum.ToString();

            if (clockNum == 0)
            {
                if (isEnable)
                {
                    CheckClockBlocking(resource);
                    parentSprite.DOFade(.4f, .4f);
                    isEnable = false;
                }
                else
                {
                    for(int i = 0; i < clockWaitingList.Count; i++)
                    {
                        clockWaitingList[i].isClockWaiting = false;
                    }
                    clockWaitingList.Clear();

                    for(int i = 0; i < resource.GetAvailableCount(); i++)
                    {
                        (Process, ProcessColor) temp = resource.GetWaitingPandPC();
                        if (temp.Item1 != null)
                        {
                            GameManager.instance.MoveToNextRound(new Todo(temp.Item1, temp.Item2), round, true);
                        }
                    }
                    parentSprite.DOFade(1f, .4f);
                    isEnable = true;
                }

                thisTransform.gameObject.SetActive(false);
                isTimeDone = true;

                return;
            }
            //if(resource.waitingQueue.Count != 0) GameManager.instance.MoveToNextRound(new Todo(resource.waitingQueue.Peek().Item1, resource.waitingQueue.Peek().Item2), round, true);
        }
        //else
        //{
        //    (Process, ProcessColor) temp = resource.GetWaitingPandPC();
        //    if (temp.Item1 != null)
        //    {
        //        temp.Item1.isClockWaiting = false;
        //        GameManager.instance.MoveToNextRound(new Todo(temp.Item1, temp.Item2), round, true);
        //    }
        //}
    }

    public void CheckClockBlocking(Resource r)
    {
        for (int i = 0; i < r.connectedProcess.Count; i++)
        {
            if (r.connectedProcess[i].Item1.GetState() != State.TERMINATE && r.connectedProcess[i].Item2.isCompleted)
            {
                GameManager.instance.isClockOver = true;
                r.connectedProcess[i].Item1.transform.GetComponent<SpriteRenderer>().DOColor(new Color(.89f, 0.21f, 0.22f), 0.15f).SetLoops(2, LoopType.Yoyo);
            }
        }
    }

    public IResourceComponent DeepCopy()
    {
        ClockComponent temp = new ClockComponent(isEnableFirst, clockNum, thisTransform);
        return temp;
    }
}
