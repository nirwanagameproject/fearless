using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * Navigasi Button Text
 * - fungsi merubah warna text untuk button sesuai navigasi
 */

public class ButtonMainMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public bool selected;

    /*
     * fungsi pewarnaan text button
     */

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        transform.Find("Text").GetComponent<Text>().color = Color.white;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if(!selected)
        transform.Find("Text").GetComponent<Text>().color = Color.black;
    }
    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        transform.Find("Text").GetComponent<Text>().color = Color.white;
    }
    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        transform.Find("Text").GetComponent<Text>().color = Color.black;
    }
}
