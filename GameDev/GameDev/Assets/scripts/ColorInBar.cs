using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class ColorInBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
{
    private static List<ColorInBar> BarColors = new List<ColorInBar>();
    public static List<Vector3> basePositions = new List<Vector3>();
    private static List<ProcessColor> pcList = new List<ProcessColor>();
    private static Process process;

    private int index;
    public X child;
    private const float sqrDistance = 500.0f;
    private Sequence NotSelectedSequence;

    public ProcessColor pc;
    private Image thisImage;
    private RectTransform thisRt;

    private void Awake()
    {
        if (BarColors.Count != 0)
        {
            BarColors.Clear();
            basePositions.Clear();
        }
    }


    private void Start()
    {
        thisRt = transform.GetChild(0).GetComponent<RectTransform>();
        BarColors.Add(this);

        basePositions.Add(thisRt.position);
        index = BarColors.Count - 1;

        thisImage = transform.GetChild(0).GetComponent<Image>();
        NotSelectedSequence = DOTween.Sequence();
        NotSelectedSequence.Append(thisImage.DOFade(0.8f, 0.6f));
        NotSelectedSequence.Join(thisRt.DOScale(1.15f, 0.6f));
        NotSelectedSequence.SetLoops(-1, LoopType.Yoyo);
        NotSelectedSequence.Pause();
        NotSelectedSequence.SetAutoKill(false);
        this.gameObject.SetActive(false);
    }

    public static void SomethingChanged(Process p)
    {
        if(process == p)
        {
            List<ProcessColor> newList = p.GetProcessColorList();
            List<ColorInBar> tempList = BarColors.ToList();
            for(int i = 0; i < pcList.Count; i++)
            {
                int newIndex = newList.LastIndexOf(pcList[i]);
                if (i != newIndex)
                {
                    BarColors[i].transform.DOMove(basePositions[newIndex], .3f);
                    tempList[newIndex] = BarColors[i];
                }
                if (BarColors[i].pc.isSelected == false) BarColors[i].NotSelectedSequence.Play();
                else
                {
                    BarColors[i].NotSelectedSequence.Pause();
                    BarColors[i].thisRt.localScale = new Vector3(1, 1, 1);
                    BarColors[i].thisImage.color = new Color(BarColors[i].thisImage.color.r, BarColors[i].thisImage.color.g, BarColors[i].thisImage.color.b, 1.0f);
                }
            }
            pcList = newList;
            BarColors = tempList;

            for (int i = 0; i < BarColors.Count; i++)
            {
                BarColors[i].index = i;
            }
        }
        else
        {
            process = p;
            pcList = p.GetProcessColorList();
            for (int i = 0; i < BarColors.Count; i++)
            {
                if (i < pcList.Count)
                {
                    BarColors[i].gameObject.SetActive(true);
                    BarColors[i].thisImage.color = pcList[i].color;
                    BarColors[i].pc = pcList[i];
                    if (BarColors[i].pc.isSelected == false) BarColors[i].NotSelectedSequence.Play();
                    else
                    {
                        BarColors[i].NotSelectedSequence.Pause();
                        BarColors[i].thisRt.localScale = new Vector3(1, 1, 1);
                        BarColors[i].thisImage.color = new Color(BarColors[i].thisImage.color.r, BarColors[i].thisImage.color.g, BarColors[i].thisImage.color.b, 1.0f);
                    }
                }
                else
                {
                    BarColors[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void SetResourceEmphasize(bool isEmp)
    {
        if (pc.r != null) pc.r.transform.GetChild(pc.r.transform.childCount - 1).gameObject.SetActive(isEmp);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        transform.SetAsLastSibling();

        SetResourceEmphasize(true);
        child.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData data)
    {
        SetResourceEmphasize(false);
        child.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData data)
    {
        child.gameObject.SetActive(false);
        if (pc.isSelected) transform.position = new Vector3(data.position.x, basePositions[0].y, transform.position.z);
        else
        {
            transform.position = new Vector3(data.position.x, data.position.y, transform.position.z);
            return;
        }
        if (transform.position.x - basePositions[index].x < 0)
        {
            if (BaseRangeCheck(index - 1))
            {
                float value = Vector3.SqrMagnitude(basePositions[index - 1] - transform.position);
                if (value <= sqrDistance)
                {
                    ChangeEach(index - 1, index);
                }
            }
        }
        else
        {
            if (BaseRangeCheck(index + 1))
            {
                float value = Vector3.SqrMagnitude(basePositions[index + 1] - transform.position);
                if (value <= sqrDistance)
                {
                    ChangeEach(index + 1, index);
                }
            }
        }
    }

    public void ChangeEach(int sideIndex, int originIndex)
    {
        if (BarColors[sideIndex].pc.isSelected == false) return;
        BarColors[sideIndex].transform.DOMove(basePositions[index], 0.3f);
        int temp = pc.order;
        pc.order = BarColors[sideIndex].pc.order;
        BarColors[sideIndex].pc.order = temp;

        ColorInBar tempObject = BarColors[index];
        BarColors[index] = BarColors[sideIndex];
        BarColors[sideIndex] = tempObject;

        temp = BarColors[index].index;
        BarColors[index].index = BarColors[sideIndex].index;
        BarColors[sideIndex].index = temp;

        process.RematchColorObjectsToCorrectPlace();

        pcList = BarColors.Where(x => x.gameObject.activeSelf == true).Select(x => x.pc).ToList();
        BarColors[originIndex].pc.r.SortConnectedProcessbyDistance();
        BarColors[sideIndex].pc.r.SortConnectedProcessbyDistance();
    }

    public void OnEndDrag(PointerEventData data)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        if(results.Count != 0)
        {
            if (results[0].gameObject.CompareTag("ProcessColorBehind"))
            {
                child.gameObject.SetActive(true);
            }
            if(pc.isSelected == false)
            {
                FindCorrectResource(results);
                if (pc.r != null) AddSelectedColor();
            }
        }
        transform.DOMove(basePositions[index], .3f);
    }

    public void AddSelectedColor()
    {
        pc.isSelected = true;
        pc.isMoved = true;
        pc.order = ++Process.colorsCurrentOrder;
        process.selectedCount++;

        pc.r.connectedProcess.Add((process, pc));
        process.RenderLine(pc, true);
        process.MoveColorObject(pc);

        GameManager.instance.HideConnectedNumber(pc.r);
        pc.r.SortConnectedProcessbyDistance();
        GameManager.instance.ShowConnectedNumber(pc.r);
    }

    public void RemoveSelectedColor()
    {
        Resource r = pc.r;
        if (pc.isSelected == true)
        {
            pc.r.connectedProcess.Remove((process, pc));
            process.selectedCount--;
        }
        pc.isSelected = false;
        pc.isMoved = true;
        pc.order = 0;

        process.RenderLine(pc, false);
        pc.r = null;
        process.MoveColorObject(pc);

        r.SortConnectedProcessbyDistance();
    }

    public void FindCorrectResource(List<RaycastResult> results)
    {
        for(int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.CompareTag("Resource"))
            {
                pc.r = results[i].gameObject.GetComponent<Resource>();
                CheckConnect(pc);
                return;
            }
        }
    }

    public bool CheckConnect(ProcessColor pc)
    {
        if (pc.r.CheckConnectable(process, pc, false) == false)
        {
            GameManager.instance.effectSounds[6].Play();
            pc.r = null;
            return false;
        }
        GameManager.instance.effectSounds[5].Play();
        if (GameManager.instance.currentLevel == 0) GameManager.instance.tutorialAnim.PressTheStartButton();
        return true;
    }

    public bool BaseRangeCheck(int index)
    {
        if (index >= 0 && index < basePositions.Count && BarColors[index].gameObject.activeSelf) return true;
        else return false;
    }

    public static void Vacate()
    {
        for(int i = 0; i < BarColors.Count; i++)
        {
            BarColors[i].gameObject.SetActive(false);
        }
        process = null;
    }
}
