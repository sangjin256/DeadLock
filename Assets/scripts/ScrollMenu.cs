using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollMenu : MonoBehaviour
{
    public List<float> pos = new List<float>();
    public Scrollbar scrollbar;
    public static float scroll_pos = 0;

    public static float distance;

    void Start()
    {
        scrollbar.value = scroll_pos;
        distance = 1f / (transform.childCount - 1f);
        for (int i = 0; i < transform.childCount; i++)
        {
            pos.Add(distance * i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            scroll_pos = scrollbar.value;
        }
        else
        {
            for (int i = 0; i < pos.Count; i++)
            {
                if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
                {
                    scrollbar.value = Mathf.Lerp(scrollbar.GetComponent<Scrollbar>().value, pos[i], 0.1f);
                    transform.GetChild(i).localScale = Vector3.Lerp(transform.GetChild(i).localScale, new Vector3(1f, 1f, 1f), 0.1f);

                    Camera.main.backgroundColor = Chapter.chapters[Chapter.thisChapterNum].color;
                    BackgroundAnim.instance.resourceA.SetColor("_EmissionColor", BackgroundAnim.instance.materialColors[(Chapter.thisChapterNum) * 2]);
                    BackgroundAnim.instance.resourceB.SetColor("_EmissionColor", BackgroundAnim.instance.materialColors[(Chapter.thisChapterNum) * 2 + 1]);
                    Color c = transform.GetChild(i).GetComponent<Image>().color;
                    transform.GetChild(i).GetComponent<Image>().color = Color.Lerp(c, new Color(c.r, c.g, c.b, 1f), 0.1f);
                    Chapter.thisChapterNum = i;
                    for (int j = 0; j < pos.Count; j++)
                    {
                        if (j != i)
                        {
                            transform.GetChild(j).localScale = Vector3.Lerp(transform.GetChild(j).localScale, new Vector3(0.8f, 0.8f, 1f), 0.1f);
                            c = transform.GetChild(j).GetComponent<Image>().color;
                            transform.GetChild(j).GetComponent<Image>().color = Color.Lerp(c, new Color(c.r, c.g, c.b, 0.3f), 0.1f);
                        }
                    }
                }
            }
        }
    }
}