using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapter : MonoBehaviour
{
    public static Chapter[] chapters = new Chapter[7];
    public int chapterNum;
    public Color color;
    public bool isLocked;

    public static int thisChapterNum = 0;

    public void Start()
    {
        if(GameManager.instance.LockedStageNum - chapterNum * 10 <= 0)
        {
            isLocked = true;
            transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);
        }
        else
        {
            isLocked = false;
            transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        }

        chapters[chapterNum] = this;
    }
}
