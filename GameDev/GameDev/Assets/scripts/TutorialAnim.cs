using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class TutorialAnim : MonoBehaviour
{
    public Sequence s;
    public Tween t;
    public RectTransform rt;
    public Image image;
    public RectTransform startButton;
    public Vector3 originPosition;
    public static bool isProcessOnceClicked;

    public void Start()
    {
        isProcessOnceClicked = false;
        originPosition = transform.position;
        s = DOTween.Sequence();
        Setup();
    }

    public void Setup()
    {
        s.AppendInterval(0.6f);
        s.Append(image.DOColor(new Color(.7f, .7f, .7f), .05f));
        s.AppendInterval(0.6f);
        s.Append(image.DOColor(new Color(0, 0, 0), .05f));
        s.SetLoops(-1, LoopType.Restart);
        s.SetAutoKill(false);
    }

    public void DragColorToResource()
    {
        if (isProcessOnceClicked) return;
        rt.anchoredPosition = new Vector2(690, -1044);
        image.color = new Color(0, 0, 0, 1);
        s.Pause();
        transform.GetChild(0).gameObject.SetActive(true);
        t = rt.DOAnchorPos(new Vector2(1200, -615), 2.5f).SetEase(Ease.OutQuad).SetLoops(-1, LoopType.Restart);
    }

    public void PressTheStartButton()
    {
        t.Kill();
        transform.GetChild(0).gameObject.SetActive(false);
        rt.anchoredPosition = new Vector2(1920 / 2 + 30, startButton.anchoredPosition.y - 1080 / 2 - 40);
        s.Play();
    }

    public void Restart()
    {
        transform.position = originPosition;
        Setup();
    }
}
