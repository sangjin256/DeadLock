using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlinkAnim : MonoBehaviour
{
    float time;

    // Update is called once per frame
    void Update()
    {
        if (time < 0.5f)
        {
            GetComponent<Text>().color = new Color(1, 1, 1, 1 - time);
        }
        else
        {
            GetComponent<Text>().color = new Color(1, 1, 1, time) ;
            if(time > 1f)
            {
                time = 0;
            }
        }

        time += Time.deltaTime;
        
    }
}
