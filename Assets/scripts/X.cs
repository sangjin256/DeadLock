using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class X : MonoBehaviour, IPointerClickHandler
{
    public ColorInBar cb;

    public void OnPointerClick(PointerEventData data)
    {
        cb.SetResourceEmphasize(false);
        cb.RemoveSelectedColor();
        gameObject.SetActive(false);
    }
}
