using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using TMPro;

public enum State { RUNNING, WAITING, FAULT, TERMINATE };

public class Process : MonoBehaviour
{
    protected int fixedNum;
    protected int simultanConnectedCount;
    protected int finishedColorCount;

    private List<ProcessColor> colorObjects;
    public static int colorsCurrentOrder;
    public int lineSortingOrder;
    public int selectedCount;
    public bool isClockWaiting;

    protected State state;
    public static Process currentProcess;

    public GameObject pointer;
    public GameObject forLineRender;
    public GameObject insideNumObject;

    private Tween waitTween;
    private Tween NotDoneTween;

    private const float radian = 25.0f * Mathf.PI / 180;
    private const float dist = 0.7f;

    public void Initialize(int fixedNum = 0)
    {
        colorsCurrentOrder = 0;
        finishedColorCount = 0;
        simultanConnectedCount = 0;
        lineSortingOrder = 0;

        colorObjects = new List<ProcessColor>();
        this.fixedNum = fixedNum;
        state = State.RUNNING;

        if(fixedNum != 0)
        {
            GameObject g = transform.GetChild(transform.childCount - 2).gameObject;
            g.GetComponent<TextMeshPro>().text = fixedNum.ToString();
            g.SetActive(true);
        }
    }

    private void OnMouseUpAsButton()
    {
        if (GameManager.instance.isStarted == false && GameManager.instance.isSettingOn == false)
        {
            GameManager.instance.effectSounds[2].Play();
            if (currentProcess != this)
            {
                if (GameManager.instance.currentLevel == 0)
                {
                    GameManager.instance.tutorialAnim.DragColorToResource();
                    TutorialAnim.isProcessOnceClicked = true;
                }
                ColorInBar.SomethingChanged(this);
                currentProcess = this;
            }
            else
            {
                ColorInBar.Vacate();
                currentProcess = null;
            }
        }
    }

    public bool CheckScheduleDone()
    {
        bool isFinished = colorObjects.Find(x => x.isSelected == false) == null;

        if (isFinished == false)
        {
            if(NotDoneTween == null) NotDoneTween = transform.GetComponent<SpriteRenderer>().DOColor(new Color(.89f, 0.21f, 0.22f), 0.15f).SetLoops(2, LoopType.Yoyo);
            else if(NotDoneTween.active != true) NotDoneTween = transform.GetComponent<SpriteRenderer>().DOColor(new Color(.89f, 0.21f, 0.22f), 0.15f).SetLoops(2, LoopType.Yoyo);
        }
        return isFinished;
    }

    public bool IsSimWaiting()
    {
        return simultanConnectedCount != 0;
    }

    public bool isFinished()
    {
        return finishedColorCount == colorObjects.Count;
    }

    public void Finish()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        
        this.state = State.TERMINATE;
        GameManager.instance.doneProcessCount++;
    }

    public void AddColorObject(Vector3 originPosition, Transform g, Color color)
    {
        ProcessColor pc = new ProcessColor(originPosition, g.transform, color);
        colorObjects.Add(pc);
        CreateLineRenderer(pc);
    }

    public void MoveColorObject(ProcessColor pc)
    {
        if (pc != null)
        {
            RematchColorObjectsToCorrectPlace();

            ColorInBar.SomethingChanged(this);
        }
    }

    public void RematchColorObjectsToCorrectPlace()
    {
        SortColorsbyOrder();
        float nowRadian;
        var selectedCount = (from colors in colorObjects
                             where colors.isSelected == true
                             select colors).Count();
        if (selectedCount % 2 == 0) nowRadian = -radian * (int)(selectedCount / 2 - 1) - radian / 2;
        else nowRadian = -radian * (int)(selectedCount / 2);

        Queue<int> returnQueue = new Queue<int>();
        for (int i = 0; i < colorObjects.Count; i++)
        {
            // selected가 false인데 움직였다면 아래로 내려온것(remove)
            if (colorObjects[i].isSelected == false)
            {
                if (colorObjects[i].isMoved)
                {
                    colorObjects[i].isMoved = false;
                    SpriteRenderer sr = colorObjects[i].thisTransform.GetComponent<SpriteRenderer>();
                    colorObjects[i].thisSequence.Kill();
                    returnQueue.Enqueue(i);
                    Tween t = sr.DOFade(0, 0.3f).OnComplete(() =>
                    {
                        int index = returnQueue.Dequeue();
                        colorObjects[index].thisTransform.position = colorObjects[index].originPosition;
                        sr.DOFade(1, 0.2f);
                    }); 
                }
            }
            else
            {
                if (colorObjects[i].isMoved)
                {
                    colorObjects[i].isMoved = false;
                    Sequence mySequence = DOTween.Sequence();
                    mySequence.Append(colorObjects[i].thisTransform.DOLocalMoveX(0.7f, 0.3f).SetEase(Ease.OutQuad));
                    mySequence.Join(colorObjects[i].thisTransform.DOLocalMoveY(0.0f, 0.3f).SetEase(Ease.InQuad));
                    mySequence.Append(colorObjects[i].thisTransform.DOLocalMoveX(dist * Mathf.Sin(nowRadian), 0.2f).SetEase(Ease.InQuad));
                    mySequence.Join(colorObjects[i].thisTransform.DOLocalMoveY(dist * Mathf.Cos(nowRadian), 0.2f).SetEase(Ease.OutQuad));

                    colorObjects[i].thisSequence = mySequence;
                }
                else
                {
                    colorObjects[i].thisTransform.DOLocalMoveX(dist * Mathf.Sin(nowRadian), 0.5f).SetEase(Ease.OutQuad);
                    colorObjects[i].thisTransform.DOLocalMoveY(dist * Mathf.Cos(nowRadian), 0.5f).SetEase(Ease.OutCubic);
                    nowRadian += radian;
                }
            }
        }
    }

    public void SortColorsbyOrder()
    {
        colorObjects = (from colors in colorObjects
                        orderby colors.order
                        select colors).ToList();
    }

    public List<ProcessColor> GetProcessColorList()
    {
        return colorObjects;
    }

    public void CreateLineRenderer(ProcessColor pc)
    {
        LineRenderer lr = Instantiate(forLineRender, transform.position, Quaternion.identity).GetComponent<LineRenderer>();
        lr.startWidth = .07f;
        lr.endWidth = .07f;
        lr.startColor = pc.color;
        lr.endColor = pc.color;
        pc.lr = lr;
    }

    public void RenderLine(ProcessColor pc, bool isActive)
    {
        if (isActive == false)
        {
            pc.lr.startColor = new Color(pc.lr.startColor.r, pc.lr.startColor.g, pc.lr.startColor.b, 1.0f);
            pc.lr.endColor = new Color(pc.lr.endColor.r, pc.lr.endColor.g, pc.lr.endColor.b, 1.0f);
            pc.lr.enabled = false;
            List<ProcessColor> temp = colorObjects.FindAll(x => pc.r.Equals(x.r));
            if (temp.Count == 3)
            {
                float opacity = 1.0f;
                for(int i = 0; i < temp.Count; i++)
                {
                    if(temp[i].Equals(pc) != true)
                    {
                        temp[i].lr.startColor = new Color(temp[i].lr.startColor.r, temp[i].lr.startColor.g, temp[i].lr.startColor.b, opacity);
                        temp[i].lr.endColor = new Color(temp[i].lr.endColor.r, temp[i].lr.endColor.g, temp[i].lr.endColor.b, opacity);
                        opacity -= 0.3f;
                    }
                }
            }
            else if(temp.Count == 2)
            {
                for(int i = 0; i < temp.Count; i++)
                {
                    if(temp.Equals(pc) != true)
                    {
                        temp[i].lr.startColor = new Color(temp[i].lr.startColor.r, temp[i].lr.startColor.g, temp[i].lr.startColor.b, 1.0f);
                        temp[i].lr.endColor = new Color(temp[i].lr.endColor.r, temp[i].lr.endColor.g, temp[i].lr.endColor.b, 1.0f);
                    }
                }
            }
            return;
        }

        pc.lr.sortingOrder = ++lineSortingOrder;
        pc.lr.SetPosition(0, transform.position);
        pc.lr.SetPosition(1, pc.r.transform.position);

        if(colorObjects.FindAll(x => pc.r == x.r).Count > 1)
        {
            pc.lr.startColor = new Color(pc.lr.startColor.r, pc.lr.startColor.g, pc.lr.startColor.b, .7f);
            pc.lr.endColor = new Color(pc.lr.endColor.r, pc.lr.endColor.g, pc.lr.endColor.b, .7f);
        }

        pc.lr.enabled = true;
    }

    public void SetAllLineRendererfalse()
    {
        for(int i = 0; i < colorObjects.Count; i++)
        {
            lineSortingOrder = 0;
            colorObjects[i].lr.startColor = new Color(colorObjects[i].lr.startColor.r, colorObjects[i].lr.startColor.g, colorObjects[i].lr.startColor.b, 1.0f);
            colorObjects[i].lr.endColor = new Color(colorObjects[i].lr.endColor.r, colorObjects[i].lr.endColor.g, colorObjects[i].lr.endColor.b, 1.0f);
            colorObjects[i].lr.enabled = false;
        }
    }

    public ProcessColor GetProcessColorAble(Resource r)
    {
        return colorObjects.Find(x => x.r == r && x.isCompleted == false);
    }

    public int GetFixedNum()
    {
        return fixedNum;
    }

    public void SimultanCountUp()
    {
        simultanConnectedCount++;
    }

    public void SimultanCountDown()
    {
        simultanConnectedCount--;
    }

    public void FinishedColorCountUp()
    {
        finishedColorCount++;
    }

    public void Wait()
    {
        transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);
        if (waitTween == null)
        {
            waitTween = transform.GetChild(transform.childCount - 1).DOScale(1.2f, 1.0f).SetLoops(-1, LoopType.Yoyo);
        }
        else waitTween.Restart();
        this.state = State.WAITING;
    }

    public void Run()
    {
        waitTween.Pause();
        transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        this.state = State.RUNNING;
    }

    public State GetState()
    {
        return state;
    }

    public void LoadForRestart()
    {
        Color c = GetComponent<SpriteRenderer>().color;
        state = State.RUNNING;
        isClockWaiting = false;
        simultanConnectedCount = 0;
        finishedColorCount = 0;
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1.0f);
        if (fixedNum != 0) transform.GetChild(transform.childCount - 2).gameObject.SetActive(true);
        for (int i = 0; i < colorObjects.Count; i++)
        {
            colorObjects[i].lr.transform.position = transform.position;
            c = colorObjects[i].thisTransform.GetComponent<SpriteRenderer>().color;
            colorObjects[i].thisTransform.GetComponent<SpriteRenderer>().color = new Color(c.r, c.g, c.b, 1.0f);
            colorObjects[i].thisTransform.gameObject.SetActive(true);
        }
        transform.GetChild(transform.childCount - 1).transform.localScale = new Vector3(1, 1, 1);
        transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        pointer.transform.position = transform.position;
        pointer.gameObject.SetActive(true);
        for(int i = 0; i < colorObjects.Count; i++)
        {
            colorObjects[i].isMoved = false;
            colorObjects[i].isSelected = true;
            colorObjects[i].isCompleted = false;
            RenderLine(colorObjects[i], true);
        }
        RematchColorObjectsToCorrectPlace();
    }

    public void SetCurrentProcessNull()
    {
        currentProcess = null;
    }
}

public class ProcessColor
{
    public Vector3 originPosition;
    public Transform thisTransform;
    
    public bool isMoved;
    public bool isSelected;
    public bool isCompleted;

    public Sequence thisSequence;
    public LineRenderer lr;
    public Resource r;
    public Color color;

    public int order;

    public ProcessColor(Vector3 originPosition, Transform thisTransform, Color c)
    {
        order = 0;
        this.originPosition = originPosition;
        this.thisTransform = thisTransform;
        isSelected = false;
        this.color = c;
        isMoved = false;
    }
}