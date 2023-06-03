using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class tips : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    bool isFinished;

    void Start()
    {
        transform.GetChild(0).GetComponent<TextMeshProUGUI>().DOFade(0.0f, 1f).SetDelay(20f).OnComplete(() => isFinished = true);
        transform.GetChild(1).GetComponent<Image>().DOFade(0.0f, 1f).SetDelay(20f);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (isFinished)
        {
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().DOFade(1.0f, 1f);
            transform.GetChild(1).GetComponent<Image>().DOFade(1.0f, 1f);
        }
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (isFinished)
        {
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().DOFade(0.0f, 1f);
            transform.GetChild(1).GetComponent<Image>().DOFade(0.0f, 1f);
        }
    }
}
