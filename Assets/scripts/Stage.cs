using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Stage : MonoBehaviour
{
    public void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (i + 1 >= GameManager.instance.LockedStageNum - (SceneManager.GetActiveScene().buildIndex - 2) * 10)
            {
                transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
                transform.GetChild(i).GetChild(1).gameObject.SetActive(true);
            }
            else
            {
                transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                transform.GetChild(i).GetChild(1).gameObject.SetActive(false);

                if (i + 1 <= GameManager.instance.finishedStage[Chapter.thisChapterNum]) transform.GetChild(i).GetChild(0).GetComponent<Image>().color = new Color(.5f, .5f, .5f);
            }
        }
    }
}
