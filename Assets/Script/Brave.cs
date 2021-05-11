using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brave : MonoBehaviour
{
    public void Mulai()
    {
        Debug.Log("Mulai Brave!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
    }
}
