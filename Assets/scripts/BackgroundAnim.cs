using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine;

public class BackgroundAnim : MonoBehaviour
{
    public static BackgroundAnim instance;

    public Material resourceA;
    public Material resourceB;

    public List<Color> materialColors;

    private void Awake()
    {
        if (instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
